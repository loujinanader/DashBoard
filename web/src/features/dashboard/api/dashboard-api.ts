import { apiRequest } from '@/shared/api/http-client';

export interface DashboardSummary {
  total: number;
  new: number;
  processing: number;
  pending: number;
  solved: number;
  closed: number;
}

export interface TeamMember {
  role?: string | null;
  id: number;
  name?: string | null;
}

export interface TicketStatus {
  id: number;
  name: string;
}

export interface Ticket {
  id: number;
  name?: string | null;
  status?: TicketStatus | null;
  is_deleted?: boolean | null;
  team?: TeamMember[];
}

/** Mirrors DashBoard/Models/Glpi/TicketsStatus.cs. */
export const TICKET_STATUSES: { id: number; label: string }[] = [
  { id: 1, label: 'New' },
  { id: 2, label: 'Processing' },
  { id: 4, label: 'Pending' },
  { id: 5, label: 'Solved' },
  { id: 6, label: 'Closed' },
];

export interface UserTicketSummary {
  userId: number;
  userName?: string | null;
  total: number;
  new: number;
  processing: number;
  pending: number;
  solved: number;
  closed: number;
  other: number;
}

export const getSummary = () => apiRequest<DashboardSummary>('/total');
export const getTickets = () => apiRequest<Ticket[]>('/tickets');
export const getTicketsByStatus = (statusId: number) => apiRequest<Ticket[]>(`/tickets/status/${statusId}`);
export const getUserSummaries = () => apiRequest<UserTicketSummary[]>('/tickets/users/totaldetails');
export const syncTickets = () => apiRequest<{ message: string }>('/sync', { method: 'POST' });
