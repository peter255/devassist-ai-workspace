export interface LoginResponse {
  token: string
  username: string
  displayName: string
  role: 'Admin' | 'User'
  expiresAt: string
}

export interface StoredAuth {
  token: string
  username: string
  displayName: string
  role: 'Admin' | 'User'
  expiresAt: string
}

export interface UserDto {
  id: string
  username: string
  displayName: string
  role: 'Admin' | 'User'
  isActive: boolean
  createdAt: string
}
