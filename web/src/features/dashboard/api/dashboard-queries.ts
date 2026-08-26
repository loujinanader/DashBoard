import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { getSummary, getTickets, getTicketsByStatus, getUserSummaries, syncTickets } from './dashboard-api';

const summaryKey = ['dashboard', 'summary'] as const;
const ticketsKey = (statusId: number | null) => ['dashboard', 'tickets', statusId] as const;
const userSummariesKey = ['dashboard', 'userSummaries'] as const;

export function useDashboardSummary() {
  return useQuery({ queryKey: summaryKey, queryFn: getSummary, refetchInterval: 60_000 });
}

export function useUserSummaries() {
  return useQuery({ queryKey: userSummariesKey, queryFn: getUserSummaries, refetchInterval: 60_000 });
}

export function useTickets(statusId: number | null) {
  return useQuery({
    queryKey: ticketsKey(statusId),
    queryFn: () => (statusId === null ? getTickets() : getTicketsByStatus(statusId)),
  });
}

export function useSyncTickets() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: syncTickets,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    },
  });
}
