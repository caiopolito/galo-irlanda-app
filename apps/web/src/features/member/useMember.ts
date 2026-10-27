import { useQuery } from "@tanstack/react-query";
import { api, unwrap } from "#/lib/api";

export const memberKeys = {
	me: ["socio", "me"] as const,
	card: ["socio", "card"] as const,
	events: ["socio", "events"] as const,
	billing: ["socio", "billing"] as const,
	polls: ["socio", "polls"] as const,
	benefits: ["socio", "benefits"] as const,
	photos: ["socio", "photos"] as const,
	documents: ["socio", "documents"] as const,
};

export const useMemberProfile = () =>
	useQuery({
		queryKey: memberKeys.me,
		queryFn: () => unwrap(api.GET("/api/socio/me")),
	});
