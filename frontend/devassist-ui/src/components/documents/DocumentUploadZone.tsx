import { useCallback, useRef, useState } from 'react'
import type { DocumentType } from '../../types/documents'
import { DocumentTypePicker } from './DocumentTypePicker'

type DocumentUploadZoneProps = {
  selectedFile: File | null
  documentType: DocumentType
  isUploading: boolean
  errorMessage?: string
  onFileSelect: (file: File | null) => void
  onDocumentTypeChange: (type: DocumentType) => void
  onUpload: () => void
}

function formatFileSize(bytes: number) {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

export function DocumentUploadZone({
  selectedFile,
  documentType,
  isUploading,
  errorMessage,
  onFileSelect,
  onDocumentTypeChange,
  onUpload,
}: DocumentUploadZoneProps) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [isDragging, setIsDragging] = useState(false)

  const handleFiles = useCallback(
    (files: FileList | null) => {
      const file = files?.[0] ?? null
      onFileSelect(file)
    },
    [onFileSelect],
  )

  const onDrop = useCallback(
    (event: React.DragEvent) => {
      event.preventDefault()
      setIsDragging(false)
      handleFiles(event.dataTransfer.files)
    },
    [handleFiles],
  )

  return (
    <section className="upload-hero">
      <div className="upload-hero__glow" aria-hidden="true" />
      <header className="upload-hero__header">
        <div>
          <p className="upload-hero__eyebrow">Knowledge base</p>
          <h2 className="upload-hero__title">Feed your copilot</h2>
          <p className="upload-hero__subtitle">
            Drop engineering docs here — specs, runbooks, postmortems — and index them for AI retrieval.
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
        className={`dropzone ${isDragging ? 'dropzone--active' : ''} ${selectedFile ? 'dropzone--filled' : ''}`}
        onDragOver={(event) => {
          event.preventDefault()
          setIsDragging(true)
        }}
        onDragLeave={() => setIsDragging(false)}
        onDrop={onDrop}
        onClick={() => inputRef.current?.click()}
        onKeyDown={(event) => {
          if (event.key === 'Enter' || event.key === ' ') {
            event.preventDefault()
            inputRef.current?.click()
          }
        }}
        role="button"
        tabIndex={0}
        aria-label="Upload document"
      >
        <input
          ref={inputRef}
          type="file"
          className="dropzone__input"
          accept=".txt,.md,.pdf,.docx"
          onChange={(event) => handleFiles(event.target.files)}
        />

        {!selectedFile ? (
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
            <p className="dropzone__title">Drag & drop your file</p>
            <p className="dropzone__hint">or click to browse — max 10 MB</p>
          </div>
        ) : (
          <div className="dropzone__file" onClick={(event) => event.stopPropagation()}>
            <div className="file-preview">
              <span className="file-preview__badge">Ready</span>
              <p className="file-preview__name">{selectedFile.name}</p>
              <p className="file-preview__meta">{formatFileSize(selectedFile.size)}</p>
            </div>
            <button
              type="button"
              className="file-preview__clear"
              onClick={() => onFileSelect(null)}
              aria-label="Remove file"
            >
              ✕
            </button>
          </div>
        )}
      </div>

      <DocumentTypePicker value={documentType} onChange={onDocumentTypeChange} />

      <div className="upload-actions">
        <button
          type="button"
          className="upload-btn"
          disabled={!selectedFile || isUploading}
          onClick={onUpload}
        >
          {isUploading ? (
            <>
              <span className="upload-btn__spinner" aria-hidden="true" />
              Uploading…
            </>
          ) : (
            <>
              <span aria-hidden="true">↑</span>
              Upload to knowledge base
            </>
          )}
        </button>
      </div>

      {errorMessage && <p className="upload-error">{errorMessage}</p>}
    </section>
  )
}
