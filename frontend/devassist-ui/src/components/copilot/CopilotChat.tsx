import { useMutation } from '@tanstack/react-query'
import { useEffect, useRef, useState } from 'react'
import { askCopilot, createChatSession } from '../../api/copilot'
import type { ChatMessageItem, Citation } from '../../types/copilot'

const SESSION_STORAGE_KEY = 'devassist.copilot.sessionId'

type CopilotChatProps = {
  hasIndexedDocuments: boolean
}

export function CopilotChat({ hasIndexedDocuments }: CopilotChatProps) {
  const [sessionId, setSessionId] = useState<string | null>(() =>
    localStorage.getItem(SESSION_STORAGE_KEY),
  )
  const [messages, setMessages] = useState<ChatMessageItem[]>([])
  const [question, setQuestion] = useState('')
  const messagesEndRef = useRef<HTMLDivElement>(null)

  const createSessionMutation = useMutation({
    mutationFn: () => createChatSession(),
    onSuccess: (data) => {
      setSessionId(data.sessionId)
      localStorage.setItem(SESSION_STORAGE_KEY, data.sessionId)
      setMessages([])
    },
  })

  const askMutation = useMutation({
    mutationFn: (text: string) => askCopilot(sessionId!, text),
    onMutate: (text) => {
      const userMsg: ChatMessageItem = {
        id: crypto.randomUUID(),
        role: 'user',
        content: text,
        createdAt: new Date().toISOString(),
      }
      setMessages((prev) => [...prev, userMsg])
      setQuestion('')
      return { userMsgId: userMsg.id, previousQuestion: text }
    },
    onSuccess: (data) => {
      const assistantMsg: ChatMessageItem = {
        id: crypto.randomUUID(),
        role: 'assistant',
        content: data.answer,
        citations: data.citations,
        createdAt: new Date().toISOString(),
      }
      setMessages((prev) => [...prev, assistantMsg])
    },
    onError: (error, _text, context) => {
      if (context?.userMsgId) {
        setMessages((prev) => prev.filter((m) => m.id !== context.userMsgId))
      }
      if (context?.previousQuestion) {
        setQuestion(context.previousQuestion)
      }

      const message = (error as Error).message.toLowerCase()
      if (message.includes('not found') || message.includes('session')) {
        setSessionId(null)
        localStorage.removeItem(SESSION_STORAGE_KEY)
      }
    },
  })

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages, askMutation.isPending])

  const handleNewSession = () => {
    createSessionMutation.mutate()
  }

  const handleAsk = (event: React.FormEvent) => {
    event.preventDefault()
    if (!sessionId || !question.trim() || askMutation.isPending) return
    askMutation.mutate(question.trim())
  }

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
            disabled={createSessionMutation.isPending}
          >
            {createSessionMutation.isPending ? 'Starting…' : sessionId ? 'New session' : 'Start session'}
          </button>
        </div>
      </header>

      {!sessionId && (
        <div className="copilot-chat__empty">
          <p>Start a chat session to ask questions about your indexed engineering documents.</p>
          <button
            type="button"
            className="session-btn session-btn--primary"
            onClick={handleNewSession}
            disabled={createSessionMutation.isPending}
          >
            Start chat session
          </button>
        </div>
      )}

      {sessionId && (
        <>
          <div className="copilot-chat__messages">
            {messages.length === 0 && (
              <p className="copilot-chat__hint">
                {hasIndexedDocuments
                  ? 'Ask about architecture, integrations, runbooks, or requirements in your indexed docs.'
                  : 'Upload and index a document first, then ask questions here.'}
              </p>
            )}

            {messages.map((message) => (
              <MessageBubble key={message.id} message={message} />
            ))}

            {askMutation.isPending && (
              <div className="message message--assistant message--typing">
                <span className="typing-dot" />
                <span className="typing-dot" />
                <span className="typing-dot" />
              </div>
            )}
            <div ref={messagesEndRef} />
          </div>

          <form className="copilot-chat__input-row" onSubmit={handleAsk}>
            <input
              type="text"
              value={question}
              onChange={(e) => setQuestion(e.target.value)}
              placeholder={
                hasIndexedDocuments
                  ? 'e.g. Summarize the authentication flow…'
                  : 'Index documents first, then ask here…'
              }
              disabled={askMutation.isPending}
            />
            <button type="submit" disabled={!question.trim() || askMutation.isPending}>
              Ask
            </button>
          </form>

          {askMutation.isError && (
            <p className="copilot-chat__error">{(askMutation.error as Error).message}</p>
          )}
        </>
      )}

      {createSessionMutation.isError && (
        <p className="copilot-chat__error">{(createSessionMutation.error as Error).message}</p>
      )}
    </section>
  )
}

function MessageBubble({ message }: { message: ChatMessageItem }) {
  return (
    <div className={`message message--${message.role}`}>
      <p className="message__content">{message.content}</p>
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
