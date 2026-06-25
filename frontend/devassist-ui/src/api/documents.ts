import type {
  DocumentDetails,
  DocumentSummary,
  DocumentType,
  IndexDocumentResponse,
  UploadDocumentResponse,
} from '../types/documents'
import { apiBaseUrl } from './client'
import { parseApiResponse } from './parseResponse'

export async function listDocuments(): Promise<DocumentSummary[]> {
  const response = await fetch(`${apiBaseUrl}/api/documents`)
  return parseApiResponse<DocumentSummary[]>(response)
}

export async function getDocument(documentId: string): Promise<DocumentDetails> {
  const response = await fetch(`${apiBaseUrl}/api/documents/${documentId}`)
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
    body: formData,
  })

  return parseApiResponse<UploadDocumentResponse>(response)
}

export async function indexDocument(documentId: string): Promise<IndexDocumentResponse> {
  const response = await fetch(`${apiBaseUrl}/api/documents/${documentId}/index`, {
    method: 'POST',
  })
  return parseApiResponse<IndexDocumentResponse>(response)
}
