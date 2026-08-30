import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { DateRange, getLocationSummaries, getSummary, getTickets, getTicketsByStatus, getUserSummaries, syncTickets } from './dashboard-api';

const summaryKey = (range: DateRange) => ['dashboard', 'summary', range] as const;
const ticketsKey = (statusId: number | null, range: DateRange) => ['dashboard', 'tickets', statusId, range] as const;
const userSummariesKey = (range: DateRange) => ['dashboard', 'userSummaries', range] as const;
const locationSummariesKey = (range: DateRange) => ['dashboard', 'locationSummaries', range] as const;

export function useDashboardSummary(range: DateRange = {}, { enabled = true }: { enabled?: boolean } = {}) {
  return useQuery({ queryKey: summaryKey(range), queryFn: () => getSummary(range), refetchInterval: 60_000, enabled });
}

export function useUserSummaries(range: DateRange = {}, { enabled = true }: { enabled?: boolean } = {}) {
  return useQuery({ queryKey: userSummariesKey(range), queryFn: () => getUserSummaries(range), refetchInterval: 60_000, enabled });
}

export function useLocationSummaries(range: DateRange = {}, { enabled = true }: { enabled?: boolean } = {}) {
  return useQuery({ queryKey: locationSummariesKey(range), queryFn: () => getLocationSummaries(range), refetchInterval: 60_000, enabled });
}

export function useTickets(statusId: number | null, range: DateRange = {}, { enabled = true }: { enabled?: boolean } = {}) {
  return useQuery({
    queryKey: ticketsKey(statusId, range),
    queryFn: () => (statusId === null ? getTickets(range) : getTicketsByStatus(statusId, range)),
    enabled,
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
