import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useCallback, useEffect, useRef, useState } from 'react'
import {
  askCopilotStream,
  createChatSession,
  getChatMessages,
  listChatSessions,
} from '../../api/copilot'
import { queryKeys } from '../../app/queryKeys'
import { useAuth } from '../../auth/authContext'
import type { ChatMessageItem, Citation } from '../../types/copilot'

function sessionStorageKey(username: string) {
  return `devassist.copilot.sessionId.${username}`
}

type CopilotChatProps = {
  hasIndexedDocuments: boolean
}

export function CopilotChat({ hasIndexedDocuments }: CopilotChatProps) {
  const { auth } = useAuth()
  const queryClient = useQueryClient()
  const username = auth?.username ?? 'anonymous'
  const storageKey = sessionStorageKey(username)

  const [sessionId, setSessionId] = useState<string | null>(() =>
    localStorage.getItem(storageKey),
  )
  const [messages, setMessages] = useState<ChatMessageItem[]>([])
  const [question, setQuestion] = useState('')
  const [isStreaming, setIsStreaming] = useState(false)
  const [streamError, setStreamError] = useState<string | null>(null)
  const [isCreatingSession, setIsCreatingSession] = useState(false)
  const messagesEndRef = useRef<HTMLDivElement>(null)
  const abortRef = useRef<AbortController | null>(null)
  const historyLoadedFor = useRef<string | null>(null)

  const sessionsQuery = useQuery({
    queryKey: queryKeys.chatSessions,
    queryFn: listChatSessions,
    staleTime: 30_000,
  })

  const historyQuery = useQuery({
    queryKey: queryKeys.sessionMessages(sessionId ?? ''),
    queryFn: () => getChatMessages(sessionId!),
    enabled: Boolean(sessionId),
    staleTime: 0,
    retry: false,
  })

  useEffect(() => {
    if (!sessionId || !historyQuery.data) return
    if (historyLoadedFor.current === sessionId) return
    historyLoadedFor.current = sessionId
    setMessages(historyQuery.data)
  }, [sessionId, historyQuery.data])

  useEffect(() => {
    if (!historyQuery.isError || !sessionId) return
    const msg = (historyQuery.error as Error).message.toLowerCase()
    if (
      msg.includes('not found') ||
      msg.includes('404') ||
      msg.includes('access denied') ||
      msg.includes('403')
    ) {
      setSessionId(null)
      localStorage.removeItem(storageKey)
      historyLoadedFor.current = null
      setMessages([])
    }
  }, [historyQuery.isError, historyQuery.error, sessionId, storageKey])

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages, isStreaming])

  const persistSession = useCallback(
    (id: string) => {
      setSessionId(id)
      localStorage.setItem(storageKey, id)
    },
    [storageKey],
  )

  const handleSelectSession = useCallback(
    (id: string) => {
      if (id === sessionId || isStreaming) return
      abortRef.current?.abort()
      setStreamError(null)
      historyLoadedFor.current = null
      setMessages([])
      persistSession(id)
    },
    [sessionId, isStreaming, persistSession],
  )

  const handleNewSession = useCallback(async () => {
    abortRef.current?.abort()
    setIsCreatingSession(true)
    setStreamError(null)
    try {
      const data = await createChatSession()
      historyLoadedFor.current = data.sessionId
      persistSession(data.sessionId)
      setMessages([])
      await queryClient.invalidateQueries({ queryKey: queryKeys.chatSessions })
    } catch (err) {
      setStreamError((err as Error).message)
    } finally {
      setIsCreatingSession(false)
    }
  }, [persistSession, queryClient])

  const handleAsk = useCallback(
    async (event: React.FormEvent) => {
      event.preventDefault()
      if (!sessionId || !question.trim() || isStreaming) return

      const text = question.trim()
      setQuestion('')
      setStreamError(null)

      const userMsg: ChatMessageItem = {
        id: crypto.randomUUID(),
        role: 'user',
        content: text,
        createdAt: new Date().toISOString(),
      }
      setMessages((prev) => [...prev, userMsg])

      const assistantId = crypto.randomUUID()
      setMessages((prev) => [
        ...prev,
        { id: assistantId, role: 'assistant', content: '', createdAt: new Date().toISOString() },
      ])
      setIsStreaming(true)

      const controller = new AbortController()
      abortRef.current = controller

      try {
        await askCopilotStream(
          sessionId,
          text,
          (token) => {
            setMessages((prev) =>
              prev.map((m) =>
                m.id === assistantId ? { ...m, content: m.content + token } : m,
              ),
            )
          },
          (citations) => {
            setMessages((prev) =>
              prev.map((m) => (m.id === assistantId ? { ...m, citations } : m)),
            )
          },
          controller.signal,
        )
        await queryClient.invalidateQueries({ queryKey: queryKeys.chatSessions })
      } catch (err) {
        if ((err as Error).name === 'AbortError') return
        const message = (err as Error).message
        setStreamError(message)
        setMessages((prev) => prev.filter((m) => m.id !== assistantId))
        setMessages((prev) => prev.filter((m) => m.id !== userMsg.id))
        setQuestion(text)

        if (message.toLowerCase().includes('not found') || message.toLowerCase().includes('session')) {
          setSessionId(null)
          localStorage.removeItem(storageKey)
          historyLoadedFor.current = null
          setMessages([])
        }
      } finally {
        setIsStreaming(false)
        abortRef.current = null
      }
    },
    [sessionId, question, isStreaming, queryClient, storageKey],
  )

  const isLoading = historyQuery.isLoading && Boolean(sessionId)
  const sessions = sessionsQuery.data ?? []

  return (
    <section className="copilot-chat">
      <header className="copilot-chat__header">
        <div>
          <p className="copilot-chat__eyebrow">Knowledge Copilot</p>
          <h3 className="copilot-chat__title">Ask your documents</h3>
        </div>
        <div className="copilot-chat__actions">
          {sessionId ? (
            <span className="session-badge" title={sessionId}>
              Session active
            </span>
          ) : null}
          <button
            type="button"
            className="session-btn"
            onClick={handleNewSession}
            disabled={isCreatingSession || isStreaming}
          >
            {isCreatingSession ? 'Starting…' : 'New session'}
          </button>
        </div>
      </header>

      <div className="copilot-chat__layout">
        <aside className="copilot-session-panel" aria-label="Chat history">
          <div className="copilot-session-panel__header">
            <h4 className="copilot-session-panel__title">
              Your sessions
              {sessions.length > 0 && (
                <span className="copilot-panel__count">{sessions.length}</span>
              )}
            </h4>
          </div>

          {sessionsQuery.isLoading && (
            <p className="copilot-session-panel__hint">Loading history…</p>
          )}
          {sessionsQuery.isError && (
            <p className="copilot-session-panel__error">
              {(sessionsQuery.error as Error).message}
            </p>
          )}
          {!sessionsQuery.isLoading && sessions.length === 0 && (
            <p className="copilot-session-panel__hint">
              No past sessions yet. Start a new chat to begin.
            </p>
          )}

          {sessions.length > 0 && (
            <ul className="copilot-session-list">
              {sessions.map((session) => (
                <li key={session.sessionId}>
                  <button
                    type="button"
                    className={`copilot-session-item${
                      sessionId === session.sessionId ? ' copilot-session-item--active' : ''
                    }`}
                    onClick={() => handleSelectSession(session.sessionId)}
                    disabled={isStreaming}
                  >
                    <span className="copilot-session-item__title">{session.title}</span>
                    <span className="copilot-session-item__meta">
                      {formatSessionDate(session.lastMessageAt ?? session.createdAt)}
                      {session.messageCount > 0 && (
                        <> · {session.messageCount} messages</>
                      )}
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </aside>

        <div className="copilot-chat__main">
          {!sessionId && (
            <div className="copilot-chat__empty">
              <p>Start a chat session or pick one from your history to continue.</p>
              <button
                type="button"
                className="session-btn session-btn--primary"
                onClick={handleNewSession}
                disabled={isCreatingSession}
              >
                Start chat session
              </button>
            </div>
          )}

          {sessionId && (
            <>
              <div className="copilot-chat__messages">
                {isLoading && (
                  <p className="copilot-chat__hint">Loading conversation history…</p>
                )}

                {!isLoading && messages.length === 0 && (
                  <p className="copilot-chat__hint">
                    {hasIndexedDocuments
                      ? 'Ask about architecture, integrations, runbooks, or requirements in your indexed docs.'
                      : 'Upload and index a document first, then ask questions here.'}
                  </p>
                )}

                {messages.map((message) => (
                  <MessageBubble
                    key={message.id}
                    message={message}
                    isStreaming={
                      isStreaming &&
                      message.role === 'assistant' &&
                      message === messages[messages.length - 1]
                    }
                  />
                ))}

                <div ref={messagesEndRef} />
              </div>

              <form className="copilot-chat__input-row" onSubmit={handleAsk}>
                <input
                  type="text"
                  dir="auto"
                  value={question}
                  onChange={(e) => setQuestion(e.target.value)}
                  placeholder={
                    hasIndexedDocuments
                      ? 'e.g. Summarize the authentication flow…'
                      : 'Index documents first, then ask here…'
                  }
                  disabled={isStreaming || isLoading}
                />
                <button type="submit" disabled={!question.trim() || isStreaming || isLoading}>
                  {isStreaming ? 'Generating…' : 'Ask'}
                </button>
              </form>

              {streamError && <p className="copilot-chat__error">{streamError}</p>}
            </>
          )}
        </div>
      </div>
    </section>
  )
}

function formatSessionDate(iso: string) {
  const date = new Date(iso)
  const now = new Date()
  const sameDay =
    date.getFullYear() === now.getFullYear() &&
    date.getMonth() === now.getMonth() &&
    date.getDate() === now.getDate()

  if (sameDay) {
    return date.toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' })
  }

  return date.toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function MessageBubble({ message, isStreaming }: { message: ChatMessageItem; isStreaming: boolean }) {
  return (
    <div className={`message message--${message.role}`}>
      <p className="message__content" dir="auto">
        {message.content}
        {isStreaming && message.content === '' && (
          <>
            <span className="typing-dot" />
            <span className="typing-dot" />
            <span className="typing-dot" />
          </>
        )}
      </p>
      {message.citations && message.citations.length > 0 && (
        <CitationsList citations={message.citations} />
      )}
    </div>
  )
}

function CitationsList({ citations }: { citations: Citation[] }) {
  return (
    <div className="citations-block">
      <span className="citations__heading">Sources</span>
      <ul className="citations">
        {citations.map((citation) => (
          <li key={`${citation.documentId}-${citation.chunkReference}`}>
            <span className="citations__doc">{citation.documentName}</span>
            <span className="citations__ref">{citation.chunkReference}</span>
          </li>
        ))}
      </ul>
    </div>
  )
}
