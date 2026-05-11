using Migration.Connectors.Targets.Aprimo.Models;
using Migration.Connectors.Targets.Aprimo.Models.Aprimo;
using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Migration.Connectors.Targets.Aprimo.Clients
{
    public interface IAprimoAssetClient
    {
        Task<IReadOnlyList<AprimoRecord>> GetAllAssetsAsync(CancellationToken ct = default);

        Task<List<string>> GetAllAssetIdsAsync(CancellationToken ct = default);

        Task<AprimoRecord?> GetAssetByAemAssetIdAsync(string aemAssetId, CancellationToken ct = default);

        Task<AprimoRecord?> GetImageSetByAemImageSetIdAsync(string aemAssetId, CancellationToken ct = default);

        Task<IReadOnlyList<AprimoRecord>> GetImageSetsMissingPreviewAsync(CancellationToken ct = default);


        Task<List<AprimoRecord?>> GetAssetsByAemAssetIdAsync(string aemAssetId, CancellationToken ct = default);

        Task<List<AprimoRecord?>> GetImageSetsByAemImageSetIdAsync(string aemAssetId, CancellationToken ct = default);

        Task<bool> DeleteAssetByAprimoIdAsync(string aprimoId, CancellationToken ct = default);

        Task<AprimoRecord?> GetAssetByAprimoIdAsync(string aprimoId, CancellationToken ct = default);

        Task<AprimoRecordCreated> UploadAzureBlobToAprimoAsync(
            BlobClient blobClient,
            string realFilename,
            string classificationId,
            CancellationToken ct = default);

        Task<AprimoRecordCreated> UploadImageSetToAprimoAsync(
            string title,
            string classificationId,
            CancellationToken ct = default);

        Task<IReadOnlyDictionary<string, AprimoClassification>> GetAllClassificationsAsync(CancellationToken ct = default);

        Task PrimeFieldIdCacheFromRecordAsync(string recordId, CancellationToken ct = default);

        Task StampAemAssetIdAsync(
                string recordId,
                string aemAssetId,
                string fieldName = "productsAEMAssetID",
                string locale = "en-US",
                CancellationToken ct = default);

        Task StampAemImageSetIdAsync(
                string recordId,
                string aemImageSetId,
                string fieldName = "productsAEMImageSetID",
                string locale = "en-US",
                CancellationToken ct = default);

        Task StampMetadataAsync(string recordId, IEnumerable<AprimoFieldUpsert> fieldsToUpsert, IEnumerable<AprimoFieldUpsert> fieldsToRemove, IEnumerable<AprimoFieldUpsert> classificationsToUpsert, IEnumerable<AprimoFieldUpsert> classificationsToRemove, CancellationToken ct = default);

        Task ClearRecordLinkMetadataAsync(string recordId, IEnumerable<AprimoFieldUpsert> fieldsToUpsert, IEnumerable<AprimoFieldUpsert> classificationsToUpsert, IEnumerable<AprimoFieldUpsert> classificationsToRemove, CancellationToken ct = default);


        Task EnsureLanguagesLoadedAsync(CancellationToken ct = default);

        string ResolveLanguageId(string? languageCode = null);

        string GetRequiredFieldId(string fieldName);

        Task<IReadOnlyList<AprimoFieldDefinition>> GetAllDefinitionsAsync(CancellationToken ct = default);

        Task UploadFileToRecordAsync(
            string recordId,
            Stream fileStream,
            string fileName,
            string contentType,
            bool setAsPreview,
            CancellationToken ct = default);

        Task UploadPreviewFileToRecordAsync(
            string recordId,
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken ct = default);

        Task<string> UploadAdditionalFileToRecordAsync(
             string recordId,
             Stream fileStream,
             string fileName,
             string contentType,
             CancellationToken ct = default);

        Task<Stream> Build3dPackageZipFromExistingRecordAsync(
                Stream glbStream,
                string glbFileName,
                Stream previewStream,
                string previewFileName,
                CancellationToken ct = default);

        Task<string> UploadNewVersionFileToRecordAsync(
            string recordId,
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken ct = default);

        Task<string> RestampMasterFileNameAsync(
                    string recordId,
                    string correctedFileName,
                    string renditionRuleRanFieldId,
                    string noOptionId,
                    CancellationToken ct = default);

        Task<IReadOnlyList<AprimoRecord>> GetAssetsBySearchAsync(
            string searchExpression,
            CancellationToken ct = default);

    }
}
