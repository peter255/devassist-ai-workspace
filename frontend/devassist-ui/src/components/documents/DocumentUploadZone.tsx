import { useCallback, useRef, useState } from 'react'
import type { DocumentType } from '../../types/documents'
import { DocumentTypePicker } from './DocumentTypePicker'

const ALLOWED_EXTENSIONS = ['.txt', '.md', '.pdf', '.docx']

type DocumentUploadZoneProps = {
  selectedFiles: File[]
  documentType: DocumentType
  isUploading: boolean
  uploadProgress?: { completed: number; total: number } | null
  errorMessage?: string
  onFilesSelect: (files: File[]) => void
  onDocumentTypeChange: (type: DocumentType) => void
  onUpload: () => void
}

function formatFileSize(bytes: number) {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

function isAllowedFile(file: File) {
  const name = file.name.toLowerCase()
  return ALLOWED_EXTENSIONS.some((ext) => name.endsWith(ext))
}

function fileKey(file: File) {
  return `${file.name}-${file.size}-${file.lastModified}`
}

export function DocumentUploadZone({
  selectedFiles,
  documentType,
  isUploading,
  uploadProgress,
  errorMessage,
  onFilesSelect,
  onDocumentTypeChange,
  onUpload,
}: DocumentUploadZoneProps) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [isDragging, setIsDragging] = useState(false)
  const [pickError, setPickError] = useState<string | null>(null)

  const addFiles = useCallback(
    (incoming: FileList | File[] | null) => {
      if (!incoming || incoming.length === 0) return

      const list = Array.from(incoming)
      const allowed = list.filter(isAllowedFile)
      const rejected = list.length - allowed.length

      if (allowed.length === 0) {
        setPickError('Supported file types: .txt, .md, .pdf, .docx')
        return
      }

      const merged = new Map(selectedFiles.map((f) => [fileKey(f), f]))
      for (const file of allowed) merged.set(fileKey(file), file)
      onFilesSelect([...merged.values()])
      setPickError(rejected > 0 ? `${rejected} file(s) skipped — unsupported type.` : null)
    },
    [onFilesSelect, selectedFiles],
  )

  const removeFile = useCallback(
    (file: File) => {
      onFilesSelect(selectedFiles.filter((f) => fileKey(f) !== fileKey(file)))
    },
    [onFilesSelect, selectedFiles],
  )

  const onDrop = useCallback(
    (event: React.DragEvent) => {
      event.preventDefault()
      setIsDragging(false)
      addFiles(event.dataTransfer.files)
    },
    [addFiles],
  )

  const hasFiles = selectedFiles.length > 0

  return (
    <section className="upload-hero">
      <div className="upload-hero__glow" aria-hidden="true" />
      <header className="upload-hero__header">
        <div>
          <p className="upload-hero__eyebrow">Knowledge base</p>
          <h2 className="upload-hero__title">Feed your copilot</h2>
          <p className="upload-hero__subtitle">
            Drop one or more engineering docs — specs, runbooks, postmortems — and index them for AI retrieval.
          </p>
        </div>
        <div className="upload-hero__stats" aria-hidden="true">
          <span>.txt</span>
          <span>.md</span>
          <span>.pdf</span>
          <span>.docx</span>
        </div>
      </header>

      <div
        className={`dropzone ${isDragging ? 'dropzone--active' : ''} ${hasFiles ? 'dropzone--filled' : ''}`}
        onDragOver={(event) => {
          event.preventDefault()
          setIsDragging(true)
        }}
        onDragLeave={() => setIsDragging(false)}
        onDrop={onDrop}
        onClick={() => !hasFiles && inputRef.current?.click()}
        onKeyDown={(event) => {
          if (!hasFiles && (event.key === 'Enter' || event.key === ' ')) {
            event.preventDefault()
            inputRef.current?.click()
          }
        }}
        role={hasFiles ? undefined : 'button'}
        tabIndex={hasFiles ? undefined : 0}
        aria-label="Upload documents"
      >
        <input
          ref={inputRef}
          type="file"
          multiple
          className="dropzone__input"
          accept=".txt,.md,.pdf,.docx"
          onChange={(event) => {
            addFiles(event.target.files)
            event.target.value = ''
          }}
        />

        {!hasFiles ? (
          <div className="dropzone__empty">
            <div className="dropzone__icon">
              <svg viewBox="0 0 48 48" fill="none" aria-hidden="true">
                <path
                  d="M24 32V16M24 16L18 22M24 16L30 22"
                  stroke="currentColor"
                  strokeWidth="2.5"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
                <path
                  d="M8 34V38C8 39.1 8.9 40 10 40H38C39.1 40 40 39.1 40 38V34"
                  stroke="currentColor"
                  strokeWidth="2.5"
                  strokeLinecap="round"
                />
              </svg>
            </div>
            <p className="dropzone__title">Drag & drop your files</p>
            <p className="dropzone__hint">or click to browse — max 10 MB per file</p>
          </div>
        ) : (
          <div className="dropzone__queue" onClick={(event) => event.stopPropagation()}>
            <div className="dropzone__queue-header">
              <span className="file-preview__badge">{selectedFiles.length} file(s) ready</span>
              <button
                type="button"
                className="dropzone__add-more"
                onClick={() => inputRef.current?.click()}
                disabled={isUploading}
              >
                + Add more
              </button>
            </div>
            <ul className="dropzone__file-list">
              {selectedFiles.map((file) => (
                <li key={fileKey(file)} className="dropzone__file-item">
                  <div className="dropzone__file-info">
                    <span className="dropzone__file-name">{file.name}</span>
                    <span className="dropzone__file-meta">{formatFileSize(file.size)}</span>
                  </div>
                  <button
                    type="button"
                    className="file-preview__clear"
                    onClick={() => removeFile(file)}
                    disabled={isUploading}
                    aria-label={`Remove ${file.name}`}
                  >
                    ✕
                  </button>
                </li>
              ))}
            </ul>
          </div>
        )}
      </div>

      <DocumentTypePicker value={documentType} onChange={onDocumentTypeChange} />

      <div className="upload-actions">
        <button
          type="button"
          className="upload-btn"
          disabled={!hasFiles || isUploading}
          onClick={onUpload}
        >
          {isUploading ? (
            <>
              <span className="upload-btn__spinner" aria-hidden="true" />
              {uploadProgress
                ? `Uploading ${uploadProgress.completed}/${uploadProgress.total}…`
                : 'Uploading…'}
            </>
          ) : (
            <>
              <span aria-hidden="true">↑</span>
              Upload {hasFiles ? `${selectedFiles.length} file${selectedFiles.length === 1 ? '' : 's'}` : 'to knowledge base'}
            </>
          )}
        </button>
      </div>

      {(errorMessage || pickError) && (
        <p className="upload-error">{errorMessage ?? pickError}</p>
      )}
    </section>
  )
}
