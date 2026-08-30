import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo, useState } from 'react'
import { getDocument, indexDocument, listDocuments, uploadDocuments } from '../api/documents'
import { queryKeys } from '../app/queryKeys'
import { CopilotChat } from '../components/copilot/CopilotChat'
import { DocumentCard } from '../components/documents/DocumentCard'
import { DocumentLibraryDrawer } from '../components/documents/DocumentLibraryDrawer'
import { DocumentUploadZone } from '../components/documents/DocumentUploadZone'
import { StateMessage } from '../components/ui/StateMessage'
import type { DocumentType } from '../types/documents'
import './knowledge-copilot.css'

export function KnowledgeCopilotPage() {
  const queryClient = useQueryClient()
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [selectedFiles, setSelectedFiles] = useState<File[]>([])
  const [uploadProgress, setUploadProgress] = useState<{ completed: number; total: number } | null>(
    null,
  )
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
    mutationFn: () =>
      uploadDocuments(selectedFiles, documentType, 'system', (completed, total) => {
        setUploadProgress({ completed, total })
      }),
    onSuccess: async () => {
      setSelectedFiles([])
      setUploadProgress(null)
      await queryClient.invalidateQueries({ queryKey: queryKeys.documents })
    },
    onError: () => setUploadProgress(null),
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

  const documents = documentsQuery.data ?? []
  const docCount = documents.length
  const indexedCount = useMemo(
    () => documents.filter((d) => d.status === 'Indexed').length,
    [documents],
  )
  const pendingCount = useMemo(
    () => documents.filter((d) => d.status === 'Uploaded' || d.status === 'Failed').length,
    [documents],
  )
  const hasIndexedDocuments = indexedCount > 0

  return (
    <div className="copilot-page copilot-page--chat-first">
      <header className="copilot-toolbar">
        <div className="copilot-toolbar__info">
          <p className="copilot-toolbar__eyebrow">Knowledge Copilot</p>
          <h2 className="copilot-toolbar__title">Ask your engineering documents</h2>
          <p className="copilot-toolbar__meta">
            {docCount} document{docCount === 1 ? '' : 's'} · {indexedCount} indexed
            {pendingCount > 0 && ` · ${pendingCount} need indexing`}
          </p>
        </div>
        <button
          type="button"
          className="copilot-toolbar__btn"
          onClick={() => setDrawerOpen(true)}
        >
          Manage documents
          {docCount > 0 && <span className="copilot-toolbar__badge">{docCount}</span>}
        </button>
      </header>

      {!hasIndexedDocuments && (
        <div className="copilot-banner" role="status">
          <p>
            Upload and index documents to get grounded answers with citations.
          </p>
          <button type="button" className="copilot-banner__action" onClick={() => setDrawerOpen(true)}>
            Open document library
          </button>
        </div>
      )}

      <CopilotChat hasIndexedDocuments={hasIndexedDocuments} />

      <DocumentLibraryDrawer open={drawerOpen} onClose={() => setDrawerOpen(false)}>
        <div className="doc-drawer__workspace">
          <div className="doc-drawer__top">
            <ol className="ui-workflow-steps copilot-workflow doc-drawer__steps">
              <li><span className="ui-workflow-steps__num">1</span> Upload a document</li>
              <li><span className="ui-workflow-steps__num">2</span> Index for search</li>
              <li><span className="ui-workflow-steps__num">3</span> Ask questions in copilot</li>
            </ol>

            <DocumentUploadZone
              selectedFiles={selectedFiles}
              documentType={documentType}
              isUploading={uploadMutation.isPending}
              uploadProgress={uploadProgress}
              errorMessage={uploadMutation.isError ? (uploadMutation.error as Error).message : undefined}
              onFilesSelect={setSelectedFiles}
              onDocumentTypeChange={setDocumentType}
              onUpload={() => selectedFiles.length > 0 && uploadMutation.mutate()}
            />
          </div>

          <div className="doc-drawer__panels">
            <section className="doc-drawer__section doc-drawer__documents">
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
              {documents.length === 0 && !documentsQuery.isLoading && (
                <StateMessage>No documents yet — upload your first file above.</StateMessage>
              )}

              {documents.length > 0 && (
                <div className="doc-list">
                  {documents.map((document) => (
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
            </section>

            <section className="doc-drawer__section doc-drawer__inspector">
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
          </section>
          </div>
        </div>
      </DocumentLibraryDrawer>
    </div>
  )
}
