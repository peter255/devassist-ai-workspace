import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { createUser, deleteUser, getUsers, resetPassword, updateUser } from '../api/auth'
import { ConfirmDialog } from '../components/ui/ConfirmDialog'
import { StateMessage } from '../components/ui/StateMessage'
import type { UserDto } from '../types/auth'
import './admin-users.css'

export function AdminUsersPage() {
  const queryClient = useQueryClient()
  const [showAddForm, setShowAddForm] = useState(false)
  const [resetTarget, setResetTarget] = useState<UserDto | null>(null)
  const [newPassword, setNewPassword] = useState('')
  const [addForm, setAddForm] = useState({ username: '', displayName: '', password: '', role: 'User' })
  const [formError, setFormError] = useState<string | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<UserDto | null>(null)

  const usersQuery = useQuery({ queryKey: ['admin', 'users'], queryFn: getUsers })

  const createMutation = useMutation({
    mutationFn: createUser,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['admin', 'users'] })
      setShowAddForm(false)
      setAddForm({ username: '', displayName: '', password: '', role: 'User' })
      setFormError(null)
    },
    onError: (err) => setFormError((err as Error).message),
  })

  const toggleActiveMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      updateUser(id, { isActive }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin', 'users'] }),
  })

  const changeRoleMutation = useMutation({
    mutationFn: ({ id, role }: { id: string; role: string }) => updateUser(id, { role }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin', 'users'] }),
  })

  const resetPasswordMutation = useMutation({
    mutationFn: ({ id, password }: { id: string; password: string }) =>
      resetPassword(id, password),
    onSuccess: () => {
      setResetTarget(null)
      setNewPassword('')
    },
  })

  const deleteMutation = useMutation({
    mutationFn: deleteUser,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['admin', 'users'] }),
  })

  return (
    <div className="admin-page">
      <div className="admin-page__header">
        <div>
          <p className="admin-page__eyebrow">Administration</p>
          <h2 className="admin-page__title">User Management</h2>
        </div>
        <button
          type="button"
          className="admin-btn admin-btn--primary"
          onClick={() => setShowAddForm(true)}
        >
          + Add user
        </button>
      </div>

      {showAddForm && (
        <div className="admin-card admin-add-form">
          <h3 className="admin-card__title">New user</h3>
          <div className="admin-form-grid">
            <div className="admin-form-field">
              <label>Username</label>
              <input
                type="text"
                value={addForm.username}
                onChange={(e) => setAddForm({ ...addForm, username: e.target.value })}
                placeholder="e.g. john.doe"
              />
            </div>
            <div className="admin-form-field">
              <label>Display name</label>
              <input
                type="text"
                value={addForm.displayName}
                onChange={(e) => setAddForm({ ...addForm, displayName: e.target.value })}
                placeholder="e.g. John Doe"
              />
            </div>
            <div className="admin-form-field">
              <label>Password</label>
              <input
                type="password"
                value={addForm.password}
                onChange={(e) => setAddForm({ ...addForm, password: e.target.value })}
                placeholder="Min. 6 characters"
              />
            </div>
            <div className="admin-form-field">
              <label>Role</label>
              <select
                value={addForm.role}
                onChange={(e) => setAddForm({ ...addForm, role: e.target.value })}
              >
                <option value="User">User</option>
                <option value="Admin">Admin</option>
              </select>
            </div>
          </div>
          {formError && <p className="admin-error">{formError}</p>}
          <div className="admin-form-actions">
            <button
              type="button"
              className="admin-btn"
              onClick={() => { setShowAddForm(false); setFormError(null) }}
            >
              Cancel
            </button>
            <button
              type="button"
              className="admin-btn admin-btn--primary"
              disabled={createMutation.isPending || !addForm.username || !addForm.password}
              onClick={() => createMutation.mutate(addForm)}
            >
              {createMutation.isPending ? 'Creating…' : 'Create user'}
            </button>
          </div>
        </div>
      )}

      {resetTarget && (
        <div className="admin-card admin-add-form">
          <h3 className="admin-card__title">Reset password — {resetTarget.displayName}</h3>
          <div className="admin-form-grid">
            <div className="admin-form-field">
              <label>New password</label>
              <input
                type="password"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                placeholder="Min. 6 characters"
                autoFocus
              />
            </div>
          </div>
          <div className="admin-form-actions">
            <button type="button" className="admin-btn" onClick={() => { setResetTarget(null); setNewPassword('') }}>
              Cancel
            </button>
            <button
              type="button"
              className="admin-btn admin-btn--primary"
              disabled={resetPasswordMutation.isPending || newPassword.length < 6}
              onClick={() => resetPasswordMutation.mutate({ id: resetTarget.id, password: newPassword })}
            >
              {resetPasswordMutation.isPending ? 'Saving…' : 'Save password'}
            </button>
          </div>
        </div>
      )}

      {usersQuery.isLoading && <StateMessage variant="loading">Loading users…</StateMessage>}
      {usersQuery.isError && (
        <StateMessage variant="error">{(usersQuery.error as Error).message}</StateMessage>
      )}

      {usersQuery.data && (
        <div className="admin-table-wrap">
          <table className="admin-table">
            <thead>
              <tr>
                <th>User</th>
                <th>Username</th>
                <th>Role</th>
                <th>Status</th>
                <th>Created</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {usersQuery.data.map((user) => (
                <tr key={user.id} className={!user.isActive ? 'admin-table__row--inactive' : ''}>
                  <td>
                    <div className="admin-user-cell">
                      <span className="admin-avatar">{user.displayName[0].toUpperCase()}</span>
                      {user.displayName}
                    </div>
                  </td>
                  <td><code>{user.username}</code></td>
                  <td>
                    <select
                      className="admin-role-select"
                      value={user.role}
                      onChange={(e) => changeRoleMutation.mutate({ id: user.id, role: e.target.value })}
                    >
                      <option value="User">User</option>
                      <option value="Admin">Admin</option>
                    </select>
                  </td>
                  <td>
                    <span className={`admin-status ${user.isActive ? 'admin-status--active' : 'admin-status--inactive'}`}>
                      {user.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td>{new Date(user.createdAt).toLocaleDateString()}</td>
                  <td>
                    <div className="admin-actions">
                      <button
                        type="button"
                        className="admin-action-btn"
                        onClick={() => toggleActiveMutation.mutate({ id: user.id, isActive: !user.isActive })}
                      >
                        {user.isActive ? 'Deactivate' : 'Activate'}
                      </button>
                      <button
                        type="button"
                        className="admin-action-btn"
                        onClick={() => { setResetTarget(user); setNewPassword('') }}
                      >
                        Reset pwd
                      </button>
                      <button
                        type="button"
                        className="admin-action-btn admin-action-btn--danger"
                        onClick={() => setDeleteTarget(user)}
                      >
                        Delete
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <ConfirmDialog
        open={Boolean(deleteTarget)}
        title={deleteTarget ? `Delete user "${deleteTarget.displayName}"?` : 'Delete user?'}
        message="This cannot be undone."
        confirmLabel="Delete"
        tone="danger"
        loading={deleteMutation.isPending}
        onConfirm={() => {
          if (!deleteTarget) return
          deleteMutation.mutate(deleteTarget.id, {
            onSuccess: () => setDeleteTarget(null),
          })
        }}
        onCancel={() => setDeleteTarget(null)}
      />
    </div>
  )
}
