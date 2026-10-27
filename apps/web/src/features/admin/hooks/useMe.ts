import { useQuery } from "@tanstack/react-query";
import { ApiError, api, unwrap } from "#/lib/api";

export const meQueryKey = ["me"] as const;

/** Usuário logado; `null` se não houver sessão. */
export function useMe() {
	return useQuery({
		queryKey: meQueryKey,
		queryFn: async () => {
			try {
				return await unwrap(api.GET("/api/auth/me"));
			} catch (error) {
				if (error instanceof ApiError && error.status === 401) return null;
				throw error;
			}
		},
	});
}
