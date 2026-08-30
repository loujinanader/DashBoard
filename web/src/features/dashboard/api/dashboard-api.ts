import { apiRequest, buildQuery } from '@/shared/api/http-client';

export interface DashboardSummary {
  total: number;
  new: number;
  processing: number;
  pending: number;
  solved: number;
  closed: number;
}

export interface DateRange extends Record<string, string | undefined> {
  dateFrom?: string;
  dateTo?: string;
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

export interface TicketLocation {
  id: number;
  name: string;
}

export interface Ticket {
  id: number;
  name?: string | null;
  status?: TicketStatus | null;
  is_deleted?: boolean | null;
  team?: TeamMember[];
  date_creation?: string | null;
  location: TicketLocation;
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

export const getSummary = (range: DateRange = {}) => apiRequest<DashboardSummary>(`/total${buildQuery(range)}`);
export const getTickets = (range: DateRange = {}) => apiRequest<Ticket[]>(`/tickets${buildQuery(range)}`);
export const getTicketsByStatus = (statusId: number, range: DateRange = {}) => apiRequest<Ticket[]>(`/tickets/status/${statusId}${buildQuery(range)}`);
export const getUserSummaries = (range: DateRange = {}) => apiRequest<UserTicketSummary[]>(`/tickets/users/totaldetails${buildQuery(range)}`);
export const syncTickets = () => apiRequest<{ message: string }>('/sync', { method: 'POST' });
