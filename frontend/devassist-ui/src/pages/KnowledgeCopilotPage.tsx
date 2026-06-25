import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { getDocument, indexDocument, listDocuments, uploadDocument } from '../api/documents'
import { queryKeys } from '../app/queryKeys'
import { DocumentCard } from '../components/documents/DocumentCard'
import { DocumentUploadZone } from '../components/documents/DocumentUploadZone'
import { CopilotChat } from '../components/copilot/CopilotChat'
import { StateMessage } from '../components/ui/StateMessage'
import type { DocumentType } from '../types/documents'
import './knowledge-copilot.css'

export function KnowledgeCopilotPage() {
  const queryClient = useQueryClient()
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [documentType, setDocumentType] = useState<DocumentType>('EngineeringSpecification')
  const [selectedDocumentId, setSelectedDocumentId] = useState<string | null>(null)
  const [indexingId, setIndexingId] = useState<string | null>(null)

  const documentsQuery = useQuery({
    queryKey: queryKeys.documents,
    queryFn: listDocuments,
  })

  const detailsQuery = useQuery({
    queryKey: queryKeys.document(selectedDocumentId ?? ''),
    queryFn: () => getDocument(selectedDocumentId!),
    enabled: Boolean(selectedDocumentId),
  })

  const uploadMutation = useMutation({
    mutationFn: () => uploadDocument(selectedFile!, documentType),
    onSuccess: async () => {
      setSelectedFile(null)
      await queryClient.invalidateQueries({ queryKey: queryKeys.documents })
    },
  })

  const indexMutation = useMutation({
    mutationFn: async (documentId: string) => {
      setIndexingId(documentId)
      return indexDocument(documentId)
    },
    onSettled: () => setIndexingId(null),
    onSuccess: async (_, documentId) => {
      await queryClient.invalidateQueries({ queryKey: queryKeys.documents })
      if (selectedDocumentId === documentId) {
        await queryClient.invalidateQueries({ queryKey: queryKeys.document(documentId) })
      }
    },
  })

  const docCount = documentsQuery.data?.length ?? 0
  const hasIndexedDocuments = documentsQuery.data?.some((d) => d.status === 'Indexed') ?? false

  return (
    <div className="copilot-page">
      <DocumentUploadZone
        selectedFile={selectedFile}
        documentType={documentType}
        isUploading={uploadMutation.isPending}
        errorMessage={uploadMutation.isError ? (uploadMutation.error as Error).message : undefined}
        onFileSelect={setSelectedFile}
        onDocumentTypeChange={setDocumentType}
        onUpload={() => selectedFile && uploadMutation.mutate()}
      />

      <ol className="ui-workflow-steps copilot-workflow">
        <li><span className="ui-workflow-steps__num">1</span> Upload a document</li>
        <li><span className="ui-workflow-steps__num">2</span> Index for search</li>
        <li><span className="ui-workflow-steps__num">3</span> Ask questions in copilot</li>
      </ol>

      <div className="copilot-workspace">
        <section className="copilot-panel">
          <h3 className="copilot-panel__title">
            Your documents
            {docCount > 0 && <span className="copilot-panel__count">{docCount}</span>}
          </h3>

          {documentsQuery.isLoading && (
            <StateMessage variant="loading">Loading library…</StateMessage>
          )}
          {documentsQuery.isError && (
            <StateMessage variant="error">
              {(documentsQuery.error as Error).message}
            </StateMessage>
          )}
          {documentsQuery.data?.length === 0 && !documentsQuery.isLoading && (
            <StateMessage>No documents yet — upload your first file above.</StateMessage>
          )}

          {documentsQuery.data && documentsQuery.data.length > 0 && (
            <div className="doc-list">
              {documentsQuery.data.map((document) => (
                <DocumentCard
                  key={document.id}
                  document={document}
                  isSelected={selectedDocumentId === document.id}
                  isIndexing={indexingId === document.id}
                  onSelect={() => setSelectedDocumentId(document.id)}
                  onIndex={() => indexMutation.mutate(document.id)}
                />
              ))}
            </div>
          )}

          {indexMutation.isError && (
            <p className="ui-error-inline">{(indexMutation.error as Error).message}</p>
          )}

          <div className="copilot-chat-divider" />

          <CopilotChat hasIndexedDocuments={hasIndexedDocuments} />
        </section>

        <aside className="copilot-panel details-panel">
          <h3 className="copilot-panel__title">Inspector</h3>

          {!selectedDocumentId && (
            <div className="details-empty">
              <span className="details-empty__icon" aria-hidden="true">◎</span>
              <p>Select a document to inspect metadata and indexing status.</p>
            </div>
          )}

          {selectedDocumentId && detailsQuery.isLoading && (
            <StateMessage variant="loading">Loading…</StateMessage>
          )}
          {selectedDocumentId && detailsQuery.isError && (
            <StateMessage variant="error">
              {(detailsQuery.error as Error).message}
            </StateMessage>
          )}

          {detailsQuery.data && (
            <dl className="details-grid">
              <div className="detail-row">
                <dt>File</dt>
                <dd>{detailsQuery.data.fileName}</dd>
              </div>
              <div className="detail-row">
                <dt>Status</dt>
                <dd>
                  <span className={`status-pill status-${detailsQuery.data.status.toLowerCase()}`}>
                    <span className="status-pill__dot" />
                    {detailsQuery.data.status}
                  </span>
                </dd>
              </div>
              <div className="detail-row">
                <dt>Type</dt>
                <dd>{detailsQuery.data.documentType.replace(/([A-Z])/g, ' $1').trim()}</dd>
              </div>
              <div className="detail-row">
                <dt>Content</dt>
                <dd>{detailsQuery.data.contentType}</dd>
              </div>
              <div className="detail-row">
                <dt>Chunks</dt>
                <dd>{detailsQuery.data.chunkCount}</dd>
              </div>
              <div className="detail-row">
                <dt>Uploaded</dt>
                <dd>
                  {new Date(detailsQuery.data.uploadedAt).toLocaleString()}
                  <br />
                  <small style={{ color: '#94a3b8' }}>by {detailsQuery.data.uploadedBy}</small>
                </dd>
              </div>
              <div className="detail-row">
                <dt>Storage</dt>
                <dd style={{ fontFamily: 'ui-monospace, monospace', fontSize: '0.8rem' }}>
                  {detailsQuery.data.blobPath}
                </dd>
              </div>
            </dl>
          )}
        </aside>
      </div>
    </div>
  )
}
