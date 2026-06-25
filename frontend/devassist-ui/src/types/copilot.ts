export interface Citation {
  documentId: string
  documentName: string
  chunkReference: string
}

export interface CreateChatSessionResponse {
  sessionId: string
  title: string
  createdAt: string
}

export interface AskCopilotResponse {
  answer: string
  citations: Citation[]
}

export interface ChatMessageItem {
  id: string
  role: 'user' | 'assistant'
  content: string
  citations?: Citation[]
  createdAt: string
}
