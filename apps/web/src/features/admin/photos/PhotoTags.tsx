import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { AdminButton } from "#/features/admin/ui/AdminButton";
import styles from "#/features/admin/ui/admin.module.css";
import { inputClass } from "#/features/admin/ui/Field";
import { api, unwrap } from "#/lib/api";

/** Marca sócios na foto, à mão. Só aparecem sócios que aceitaram ser marcados. */
export function PhotoTags({ photoId }: { photoId: string }) {
	const queryClient = useQueryClient();
	const [search, setSearch] = useState("");
	const tags = useQuery({
		queryKey: ["photos", "tags", photoId],
		queryFn: () =>
			unwrap(
				api.GET("/api/admin/photos/{id}/tags", {
					params: { path: { id: photoId } },
				}),
			),
	});
	const candidates = useQuery({
		queryKey: ["members", "taggable", search],
		queryFn: () =>
			unwrap(
				api.GET("/api/admin/members", {
					params: {
						query: {
							search: search || undefined,
							photoTagOptIn: true,
							PageSize: 10,
						},
					},
				}),
			),
		enabled: search.length >= 2,
	});
	const save = useMutation({
		mutationFn: (userIds: string[]) =>
			unwrap(
				api.PUT("/api/admin/photos/{id}/tags", {
					params: { path: { id: photoId } },
					body: { userIds },
				}),
			),
		onSuccess: (tagged) =>
			queryClient.setQueryData(["photos", "tags", photoId], tagged),
	});
	const current = tags.data?.map((t) => t.userId) ?? [];

	return (
		<div className={styles.field}>
			<span className={styles.label}>Sócios na foto</span>
			<div className={styles.badges}>
				{tags.data?.map((tag) => (
					<AdminButton
						key={tag.userId}
						variant="secondary"
						onClick={() =>
							save.mutate(current.filter((id) => id !== tag.userId))
						}
					>
						{tag.fullName} ✕
					</AdminButton>
				))}
				{tags.data?.length === 0 && (
					<span className={styles.hint}>Ninguém marcado.</span>
				)}
			</div>
			<input
				className={inputClass(false)}
				placeholder="Buscar sócio para marcar"
				value={search}
				onChange={(e) => setSearch(e.target.value)}
			/>
			{candidates.data?.items
				.filter((m) => !current.includes(m.userId))
				.map((member) => (
					<AdminButton
						key={member.userId}
						variant="ghost"
						onClick={() => {
							save.mutate([...current, member.userId]);
							setSearch("");
						}}
					>
						+ {member.fullName} (nº {member.memberNumber})
					</AdminButton>
				))}
			<span className={styles.hint}>
				Marcação manual, sem reconhecimento facial. Só sócios que aceitaram ser
				marcados aparecem.
			</span>
		</div>
	);
}
