export type TaxonomyTargetType = "Bynder" | "Cloudinary" | "Aprimo";

export interface TaxonomyTargetOption {
  value: TaxonomyTargetType;
  label: string;
}

export interface TaxonomyExportRequest {
  targetType: TaxonomyTargetType;
  credentialSetId?: string;
  includeOptions: boolean;
  includeRaw: boolean;
}

export async function getTaxonomyTargets(): Promise<TaxonomyTargetOption[]> {
  const response = await fetch("/api/taxonomy/targets");

  if (!response.ok) {
    throw new Error(await response.text());
  }

  return response.json();
}

export async function exportTaxonomyExcel(request: TaxonomyExportRequest): Promise<void> {
  const response = await fetch("/api/taxonomy/export", {
    method: "POST",
    headers: {
      "Content-Type": "application/json"
    },
    body: JSON.stringify(request)
  });

  if (!response.ok) {
    throw new Error(await response.text());
  }

  const blob = await response.blob();
  const contentDisposition = response.headers.get("content-disposition") ?? "";
  const fileName = getFileNameFromContentDisposition(contentDisposition)
    ?? `${request.targetType.toLowerCase()}-taxonomy.xlsx`;

  const url = window.URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = fileName;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  window.URL.revokeObjectURL(url);
}

function getFileNameFromContentDisposition(contentDisposition: string): string | null {
  const utf8Match = /filename\*=UTF-8''([^;]+)/i.exec(contentDisposition);
  if (utf8Match?.[1]) {
    return decodeURIComponent(utf8Match[1].replace(/"/g, ""));
  }

  const asciiMatch = /filename="?([^";]+)"?/i.exec(contentDisposition);
  return asciiMatch?.[1] ?? null;
}
