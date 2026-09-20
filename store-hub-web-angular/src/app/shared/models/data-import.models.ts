export type DataImportKind = 'Categories' | 'Products' | 'Stores' | 'Users' | 'All';

export interface DataImportResultDto {
  created: number;
  skipped: number;
  failed: number;
  errors: readonly string[];
}

export function downloadBlob(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = fileName;
  a.click();
  URL.revokeObjectURL(url);
}
