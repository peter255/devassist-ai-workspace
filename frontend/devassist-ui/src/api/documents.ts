import type {
  DocumentDetails,
  DocumentSummary,
  DocumentType,
  IndexDocumentResponse,
  UploadDocumentResponse,
} from '../types/documents'
import { apiBaseUrl, getAuthHeaders } from './client'
import { parseApiResponse } from './parseResponse'

export async function listDocuments(): Promise<DocumentSummary[]> {
  const response = await fetch(`${apiBaseUrl}/api/documents`, {
    headers: await getAuthHeaders(),
  })
  return parseApiResponse<DocumentSummary[]>(response)
}

export async function getDocument(documentId: string): Promise<DocumentDetails> {
  const response = await fetch(`${apiBaseUrl}/api/documents/${documentId}`, {
    headers: await getAuthHeaders(),
  })
  return parseApiResponse<DocumentDetails>(response)
}

export async function uploadDocument(
  file: File,
  documentType: DocumentType,
  uploadedBy = 'system',
): Promise<UploadDocumentResponse> {
  const formData = new FormData()
  formData.append('file', file)
  formData.append('documentType', documentType)
  formData.append('uploadedBy', uploadedBy)

  const response = await fetch(`${apiBaseUrl}/api/documents/upload`, {
    method: 'POST',
    headers: await getAuthHeaders(),
    body: formData,
  })

  return parseApiResponse<UploadDocumentResponse>(response)
}

export async function uploadDocuments(
  files: File[],
  documentType: DocumentType,
  uploadedBy = 'system',
  onProgress?: (completed: number, total: number) => void,
): Promise<UploadDocumentResponse[]> {
  const results: UploadDocumentResponse[] = []
  for (let i = 0; i < files.length; i++) {
    results.push(await uploadDocument(files[i], documentType, uploadedBy))
    onProgress?.(i + 1, files.length)
  }
  return results
}

export async function indexDocument(documentId: string): Promise<IndexDocumentResponse> {
  const response = await fetch(`${apiBaseUrl}/api/documents/${documentId}/index`, {
    method: 'POST',
    headers: await getAuthHeaders(),
  })
  return parseApiResponse<IndexDocumentResponse>(response)
}
