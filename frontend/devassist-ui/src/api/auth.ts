import type { LoginResponse, UserDto } from '../types/auth'
import { apiBaseUrl, getAuthHeaders } from './client'
import { parseApiResponse } from './parseResponse'

export async function login(username: string, password: string): Promise<LoginResponse> {
  const response = await fetch(`${apiBaseUrl}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password }),
  })
  return parseApiResponse<LoginResponse>(response)
}

export async function getUsers(): Promise<UserDto[]> {
  const response = await fetch(`${apiBaseUrl}/api/admin/users`, {
    headers: await getAuthHeaders(),
  })
  return parseApiResponse<UserDto[]>(response)
}

export async function createUser(data: {
  username: string
  displayName: string
  password: string
  role: string
}): Promise<UserDto> {
  const response = await fetch(`${apiBaseUrl}/api/admin/users`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...(await getAuthHeaders()) },
    body: JSON.stringify(data),
  })
  return parseApiResponse<UserDto>(response)
}

export async function updateUser(id: string, data: { displayName?: string; role?: string; isActive?: boolean }): Promise<UserDto> {
  const response = await fetch(`${apiBaseUrl}/api/admin/users/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', ...(await getAuthHeaders()) },
    body: JSON.stringify(data),
  })
  return parseApiResponse<UserDto>(response)
}

export async function resetPassword(id: string, newPassword: string): Promise<void> {
  const response = await fetch(`${apiBaseUrl}/api/admin/users/${id}/reset-password`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...(await getAuthHeaders()) },
    body: JSON.stringify({ newPassword }),
  })
  await parseApiResponse<object>(response)
}

export async function deleteUser(id: string): Promise<void> {
  const response = await fetch(`${apiBaseUrl}/api/admin/users/${id}`, {
    method: 'DELETE',
    headers: await getAuthHeaders(),
  })
  await parseApiResponse<object>(response)
}
