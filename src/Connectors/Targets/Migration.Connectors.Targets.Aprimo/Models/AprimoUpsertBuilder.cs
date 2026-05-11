using Migration.Shared.Workflows.AemToAprimo.Models;
using Migration.Connectors.Targets.Aprimo.Clients;
using Migration.Connectors.Targets.Aprimo.Models.Aprimo;
using Migration.Connectors.Targets.Aprimo.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using static OfficeOpenXml.ExcelErrorValue;

namespace Migration.Connectors.Targets.Aprimo.Models
{
    public static class AprimoUpsertBuilder
    {
        //  invariant languageId is all zeros.
        public const string InvariantLanguageId = "00000000000000000000000000000000";

        public static IEnumerable<AprimoFieldUpsert> BuildUpserts(
            AssetMetadata metadata,
            IAprimoAssetClient assetClient,
            IReadOnlyList<AprimoFieldDefinition> definitions,
            IReadOnlyDictionary<string, AprimoClassification> classifications,
            List<string> output,
            string? languageId = null)
        {
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));

            var lang = string.IsNullOrWhiteSpace(languageId) ? InvariantLanguageId : languageId!;
            var upserts = new List<AprimoFieldUpsert>();

            foreach (var prop in typeof(AssetMetadata).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var attr = prop.GetCustomAttribute<AprimoFieldAttribute>(inherit: true);
                if (attr == null) continue;

                var raw = prop.GetValue(metadata) as string;
                if (string.IsNullOrWhiteSpace(raw)) continue;

                raw = raw.Trim();

                try
                {
                    if (IsListType(attr.DataType))
                    {
                        var values = SplitToValues(raw);
                        if (values.Count == 0) continue;

                        if (attr.DataType == "Classification List")
                        {
                            var def = definitions.FirstOrDefault(d => string.Equals(d.Name, attr.FieldName, StringComparison.OrdinalIgnoreCase));

                            if (def != null)
                            {
                                var classification = classifications.Values
                                    .FirstOrDefault(c =>
                                        c.Id.Equals(def.RootId));


                                var newValues = new List<string>();
                                foreach (var value in values)
                                {
                                    var labelClassification = classification.Embedded.Children.Items
                                        .FirstOrDefault(c =>
                                            c.Labels.Any(l =>
                                                string.Equals(l.Value, value,
                                                              StringComparison.OrdinalIgnoreCase)));

                                    if (labelClassification != null)
                                    {
                                        newValues.Add(labelClassification.Id);
                                    } else
                                    {
                                        var nameClassification = classification.Embedded.Children.Items
                                            .FirstOrDefault(c =>
                                                c.Name.Contains($"_{value}", StringComparison.OrdinalIgnoreCase));

                                        if (nameClassification != null)
                                        {
                                            newValues.Add(nameClassification.Id);
                                        }
                                        else
                                        {
                                            throw new ArgumentException($"No classification id could be found for field name {attr.FieldName}. Classification List does not contain value {value}");
                                        }

                                    }
                                        
                                }
                                values = newValues;

                                ;
                                upserts.Add(new AprimoFieldUpsert
                                {
                                    FieldLabel = def.Label,
                                    FieldName = attr.FieldName,
                                    FieldId = def.Id, //assetClient.GetRequiredFieldId(attr.FieldName),
                                    LanguageId = lang,
                                    //Values = values,
                                    Value = values.FirstOrDefault(),
                                    RawValue = raw,
                                    IsClassification = true
                                });
                            }
                            else
                            {
                                throw new ArgumentException($"No definition could be found for field name {attr.FieldName}.");
                            }

                        }

                        if (attr.DataType == "Option List")
                        {
                            var def = definitions.FirstOrDefault(d => string.Equals(d.Name, attr.FieldName, StringComparison.OrdinalIgnoreCase));

                            if (def != null)
                            {
                                var newValues = new List<string>();
                                foreach (var value in values)
                                {
                                    var labelClassification = def.OptionItems
                                        .FirstOrDefault(c =>
                                            c.Labels.Any(l =>
                                                string.Equals(l.Value, value,
                                                              StringComparison.OrdinalIgnoreCase)));

                                    if (labelClassification != null)
                                    {
                                        newValues.Add(labelClassification.Id);
                                    }
                                    else
                                    {
                                        throw new ArgumentException($"No option id could be found for field name {attr.FieldName}. Option List does not contain value {value}");
                                    }

                                }
                                values = newValues;

                                ;
                                upserts.Add(new AprimoFieldUpsert
                                {
                                    FieldId = def.Id, //assetClient.GetRequiredFieldId(attr.FieldName),
                                    LanguageId = lang,
                                    Values = values
                                });
                            }
                            else
                            {
                                throw new ArgumentException($"No definition could be found for field name {attr.FieldName}.");
                            }

                        }

                        if (attr.DataType == "Text List")
                        {
                            var def = definitions.FirstOrDefault(d => string.Equals(d.Name, attr.FieldName, StringComparison.OrdinalIgnoreCase));
                            if (def != null)
                            {
                                upserts.Add(new AprimoFieldUpsert
                                {
                                    FieldId = def.Id, //assetClient.GetRequiredFieldId(attr.FieldName),
                                    LanguageId = lang,
                                    Values = values
                                });
                            }
                            else
                            {
                                throw new ArgumentException($"No definition could be found for field name {attr.FieldName}.");
                            }
                        }

                    }
                    else if (attr.DataType == "Date Time")
                    {
                        var dt = DateTime.ParseExact(
                            raw,
                            "ddd MMM dd yyyy HH:mm:ss 'GMT'zzz",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AdjustToUniversal);

                        var aprimoValue = dt.ToString("yyyy-MM-ddTHH:mm:ssZ");

                        var def = definitions.FirstOrDefault(d => string.Equals(d.Name, attr.FieldName, StringComparison.OrdinalIgnoreCase));

                        if (def != null)
                        {
                            upserts.Add(new AprimoFieldUpsert
                            {
                                FieldId = def.Id, //assetClient.GetRequiredFieldId(attr.FieldName),
                                LanguageId = lang,
                                Value = aprimoValue
                            });
                        }
                        else
                        {
                            throw new ArgumentException($"No definition could be found for field name {attr.FieldName}.");
                        }

                    }
                    else
                    {
                        var def = definitions.FirstOrDefault(d => string.Equals(d.Name, attr.FieldName, StringComparison.OrdinalIgnoreCase));

                        if (def != null)
                        {
                            upserts.Add(new AprimoFieldUpsert
                            {
                                FieldId = def.Id, //assetClient.GetRequiredFieldId(attr.FieldName),
                                LanguageId = lang,
                                Value = raw
                            });
                        } else
                        {
                            throw new ArgumentException($"No definition could be found for field name {attr.FieldName}.");
                        }

                    }
                }
                catch (Exception ex)
                {
                    output.Add(ex.Message);
                    Console.WriteLine($"Cannot process Upsert for fieldId {attr.FieldName} : {ex.Message}");
                }


            }

            return upserts;
        }

        public static IEnumerable<AprimoFieldUpsert> BuildUpserts(
            AprimoImageSet metadata,
            IAprimoAssetClient assetClient,
            IReadOnlyList<AprimoFieldDefinition> definitions,
            IReadOnlyDictionary<string, AprimoClassification> classifications,
            List<string> output,
            string? languageId = null)
        {
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));

            var lang = string.IsNullOrWhiteSpace(languageId) ? InvariantLanguageId : languageId!;
            var upserts = new List<AprimoFieldUpsert>();

            foreach (var prop in typeof(AprimoImageSet).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var attr = prop.GetCustomAttribute<AprimoFieldAttribute>(inherit: true);
                if (attr == null) continue;

                var propValue = prop.GetValue(metadata);
                if (propValue == null) continue;

                string raw = string.Empty;
                if (propValue is not string)
                {
                    // AprimoImageSetAssets
                } else
                {
                    raw = Convert.ToString(propValue)?.Trim();
                    if (string.IsNullOrWhiteSpace(raw)) continue;
                }

                try
                {
                    if (IsListType(attr.DataType))
                    {
                        var values = SplitToValues(raw);
                        if (values.Count == 0) continue;

                        if (attr.DataType == "Classification List")
                        {
                            var def = definitions.FirstOrDefault(d => string.Equals(d.Name, attr.FieldName, StringComparison.OrdinalIgnoreCase));

                            if (def != null)
                            {
                                var classification = classifications.Values
                                    .FirstOrDefault(c =>
                                        c.Id.Equals(def.RootId));


                                var newValues = new List<string>();
                                foreach (var value in values)
                                {
                                    var labelClassification = classification.Embedded.Children.Items
                                        .FirstOrDefault(c =>
                                            c.Labels.Any(l =>
                                                string.Equals(l.Value, value,
                                                              StringComparison.OrdinalIgnoreCase)));

                                    if (labelClassification != null)
                                    {
                                        newValues.Add(labelClassification.Id);
                                    }
                                    else
                                    {
                                        var nameClassification = classification.Embedded.Children.Items
                                            .FirstOrDefault(c =>
                                                c.Name.Contains($"_{value}", StringComparison.OrdinalIgnoreCase));

                                        if (nameClassification != null)
                                        {
                                            newValues.Add(nameClassification.Id);
                                        }
                                        else
                                        {
                                            throw new ArgumentException($"No classification id could be found for field name {attr.FieldName}. Classification List does not contain value {value}");
                                        }

                                    }

                                }
                                values = newValues;

                                ;
                                upserts.Add(new AprimoFieldUpsert
                                {
                                    FieldLabel = def.Label,
                                    FieldName = attr.FieldName,
                                    FieldId = def.Id, //assetClient.GetRequiredFieldId(attr.FieldName),
                                    LanguageId = lang,
                                    //Values = values,
                                    Value = values.FirstOrDefault(),
                                    RawValue = raw,
                                    IsClassification = true
                                });
                            }
                            else
                            {
                                throw new ArgumentException($"No definition could be found for field name {attr.FieldName}.");
                            }

                        }

                        if (attr.DataType == "Option List")
                        {
                            var def = definitions.FirstOrDefault(d => string.Equals(d.Name, attr.FieldName, StringComparison.OrdinalIgnoreCase));

                            if (def != null)
                            {
                                var newValues = new List<string>();
                                foreach (var value in values)
                                {
                                    var labelClassification = def.OptionItems
                                        .FirstOrDefault(c =>
                                            c.Labels.Any(l =>
                                                string.Equals(l.Value, value,
                                                              StringComparison.OrdinalIgnoreCase)));

                                    if (labelClassification != null)
                                    {
                                        newValues.Add(labelClassification.Id);
                                    }
                                    else
                                    {
                                        throw new ArgumentException($"No option id could be found for field name {attr.FieldName}. Option List does not contain value {value}");
                                    }

                                }
                                values = newValues;

                                ;
                                upserts.Add(new AprimoFieldUpsert
                                {
                                    FieldLabel = def.Label,
                                    FieldName = attr.FieldName,
                                    FieldId = def.Id, //assetClient.GetRequiredFieldId(attr.FieldName),
                                    LanguageId = lang,
                                    Values = values
                                });
                            }
                            else
                            {
                                throw new ArgumentException($"No definition could be found for field name {attr.FieldName}.");
                            }

                        }

                        if (attr.DataType == "Text List")
                        {
                            var def = definitions.FirstOrDefault(d => string.Equals(d.Name, attr.FieldName, StringComparison.OrdinalIgnoreCase));
                            if (def != null)
                            {
                                upserts.Add(new AprimoFieldUpsert
                                {
                                    FieldLabel = def.Label,
                                    FieldName = attr.FieldName,
                                    FieldId = def.Id, //assetClient.GetRequiredFieldId(attr.FieldName),
                                    LanguageId = lang,
                                    Values = values
                                });
                            }
                            else
                            {
                                throw new ArgumentException($"No definition could be found for field name {attr.FieldName}.");
                            }
                        }

                    }
                    else if (attr.DataType == "Date Time")
                    {
                        var dt = DateTime.ParseExact(
                            raw,
                            "ddd MMM dd yyyy HH:mm:ss 'GMT'zzz",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AdjustToUniversal);

                        var aprimoValue = dt.ToString("yyyy-MM-ddTHH:mm:ssZ");

                        var def = definitions.FirstOrDefault(d => string.Equals(d.Name, attr.FieldName, StringComparison.OrdinalIgnoreCase));

                        if (def != null)
                        {
                            upserts.Add(new AprimoFieldUpsert
                            {
                                FieldLabel = def.Label,
                                FieldName = attr.FieldName,
                                FieldId = def.Id, //assetClient.GetRequiredFieldId(attr.FieldName),
                                LanguageId = lang,
                                Value = aprimoValue
                            });
                        }
                        else
                        {
                            throw new ArgumentException($"No definition could be found for field name {attr.FieldName}.");
                        }

                    }
                    else if (attr.DataType == "RecordLink")
                    {

                        var def = definitions.FirstOrDefault(d => string.Equals(d.Name, attr.FieldName, StringComparison.OrdinalIgnoreCase));

                        if (def != null)
                        {
                            upserts.Add(new AprimoFieldUpsert
                            {
                                FieldLabel = def.Label,
                                FieldName = attr.FieldName,
                                FieldId = def.Id, //assetClient.GetRequiredFieldId(attr.FieldName),
                                LanguageId = lang,
                                Values = metadata.AprimoImageSetAssets.AprimoRecords,
                                IsRecordLink = true
                            });
                        }
                        else
                        {
                            throw new ArgumentException($"No definition could be found for field name {attr.FieldName}.");
                        }

                    }
                    else
                    {
                        var def = definitions.FirstOrDefault(d => string.Equals(d.Name, attr.FieldName, StringComparison.OrdinalIgnoreCase));

                        if (def != null)
                        {
                            upserts.Add(new AprimoFieldUpsert
                            {
                                FieldLabel = def.Label,
                                FieldName = attr.FieldName,
                                FieldId = def.Id, //assetClient.GetRequiredFieldId(attr.FieldName),
                                LanguageId = lang,
                                Value = raw
                            });
                        }
                        else
                        {
                            output.Add($"No definition could be found for field name {attr.FieldName}.");
                            throw new ArgumentException($"No definition could be found for field name {attr.FieldName}.");
                        }

                    }
                }
                catch (Exception ex)
                {
                    output.Add($"Cannot process Upsert for fieldId {attr.FieldName} : {ex.Message}");
                    Console.WriteLine($"Cannot process Upsert for fieldId {attr.FieldName} : {ex.Message}");
                }


            }

            return upserts;
        }


        public static IEnumerable<AprimoFieldUpsert> BuildClassificationUpserts(
            Dictionary<string, string> classificationsToUpdate,
            IAprimoAssetClient assetClient,
            IReadOnlyList<AprimoFieldDefinition> definitions,
            IReadOnlyDictionary<string, AprimoClassification> classifications,
            List<string> output,
            string? languageId = null)
        {
            if (classificationsToUpdate == null) throw new ArgumentNullException(nameof(classificationsToUpdate));

            var lang = string.IsNullOrWhiteSpace(languageId) ? InvariantLanguageId : languageId!;
            var upserts = new List<AprimoFieldUpsert>();

            foreach (var key in classificationsToUpdate.Keys)
            {
                if (string.IsNullOrWhiteSpace(classificationsToUpdate[key])) continue;

                var value = classificationsToUpdate[key];
                var values = new List<string>();
                try
                {
                    var classification = classifications.Values
                        .FirstOrDefault(c =>
                            c.Labels.Any(l =>
                                string.Equals(l.Value, key,
                                              StringComparison.OrdinalIgnoreCase)));


                    var labelClassification = classification.Embedded.Children.Items
                        .FirstOrDefault(c =>
                            c.Labels.Any(l =>
                                string.Equals(l.Value, value,
                                                StringComparison.OrdinalIgnoreCase)));

                    if (labelClassification != null)
                    {
                        values.Add(labelClassification.Id);
                    }
                    else
                    {
                        var nameClassification = classification.Embedded.Children.Items
                            .FirstOrDefault(c =>
                                c.Name.Contains($"_{value}", StringComparison.OrdinalIgnoreCase));

                        if (nameClassification != null)
                        {
                            values.Add(nameClassification.Id);
                        }
                        else
                        {
                            throw new ArgumentException($"No classification id could be found for field name {key}. Classification List does not contain value {value}");
                        }

                    }

                    upserts.Add(new AprimoFieldUpsert
                    {
                        FieldLabel = key,
                        LanguageId = lang,
                        Value = values.FirstOrDefault()
                    });
                    ;
                   

                }
                catch (Exception ex)
                {
                    output.Add(ex.Message);
                    Console.WriteLine($"Cannot process Upsert for fieldId {key} : {ex.Message}");
                }


            }

            return upserts;
        }

        private static bool IsListType(string? dataType)
        {
            if (string.IsNullOrWhiteSpace(dataType)) return false;

            // These are the Aprimo types 
            return dataType.Equals("Text List", StringComparison.OrdinalIgnoreCase)
                || dataType.Equals("Option List", StringComparison.OrdinalIgnoreCase)
                || dataType.Equals("Classification List", StringComparison.OrdinalIgnoreCase);
        }

        private static List<string> SplitToValues(string raw)
        {
            // tune delimiters to match your Excel/json conventions.
            // Comma/semicolon are common for list-like fields.
            return raw
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }


    }
}
