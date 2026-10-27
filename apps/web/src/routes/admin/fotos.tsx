import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { type ChangeEvent, useState } from "react";
import { StatusPill } from "#/components/ui/StatusPill";
import { PhotoEditor } from "#/features/admin/photos/PhotoEditor";
import styles from "#/features/admin/ui/admin.module.css";
import { CONSENT_BASIS, PHOTO_VISIBILITY } from "#/features/admin/ui/labels";
import { Notice } from "#/features/admin/ui/Notice";
import { PageHeader } from "#/features/admin/ui/PageHeader";
import { Pager } from "#/features/admin/ui/Pager";
import { ApiError, api, photoUrl, unwrap, uploadPhoto } from "#/lib/api";
import { cx } from "#/lib/cx";

export const Route = createFileRoute("/admin/fotos")({ component: Photos });

function Photos() {
	const queryClient = useQueryClient();
	const [page, setPage] = useState(1);
	const [onlyWall, setOnlyWall] = useState(false);
	const [selected, setSelected] = useState<string | null>(null);

	const photos = useQuery({
		queryKey: ["photos", onlyWall, page],
		queryFn: () =>
			unwrap(
				api.GET("/api/admin/photos", {
					params: { query: { wall: onlyWall, Page: page, PageSize: 48 } },
				}),
			),
	});

	const upload = useMutation({
		mutationFn: async (files: File[]) => {
			const uploaded = [];
			for (const file of files) uploaded.push(await uploadPhoto(file));
			return uploaded;
		},
		onSuccess: async (uploaded) => {
			await queryClient.invalidateQueries({ queryKey: ["photos"] });
			if (uploaded.length === 1) setSelected(uploaded[0].id);
		},
	});
	const onFiles = (event: ChangeEvent<HTMLInputElement>) => {
		const files = Array.from(event.target.files ?? []);
		if (files.length > 0) upload.mutate(files);
		event.target.value = "";
	};

	return (
		<div className={styles.page}>
			<PageHeader
				title="Fotos"
				subtitle="Fotos chegam privadas. Para ir ao site: descrição + consentimento de quem aparece."
				actions={
					<label className={cx(styles.button, styles.primary)}>
						{upload.isPending ? "Enviando…" : "Enviar fotos"}
						<input
							type="file"
							accept="image/jpeg,image/png,image/webp"
							multiple
							hidden
							onChange={onFiles}
							disabled={upload.isPending}
						/>
					</label>
				}
			/>
			{upload.isError && (
				<Notice tone="error">
					{upload.error instanceof ApiError
						? (upload.error.field("file") ?? upload.error.summary)
						: "Falha no envio."}
				</Notice>
			)}
			<label className={styles.checkbox}>
				<input
					type="checkbox"
					checked={onlyWall}
					onChange={(e) => {
						setOnlyWall(e.target.checked);
						setPage(1);
					}}
				/>
				Só as do mural da home, na ordem
			</label>

			<div className={styles.split}>
				<div className={styles.page}>
					<div className={styles.photoGrid}>
						{photos.data?.items.map((photo) => (
							<button
								key={photo.id}
								type="button"
								className={cx(
									styles.photoTile,
									photo.id === selected && styles.photoTileSelected,
								)}
								onClick={() => setSelected(photo.id)}
							>
								<img
									src={photoUrl(photo.baseUrl)}
									alt=""
									className={styles.thumb}
									loading="lazy"
								/>
								<span className={styles.photoTileLabel}>
									{photo.title ?? "Sem título"}
								</span>
								<span
									className={styles.badges}
									style={{ padding: "0 4px 6px" }}
								>
									<StatusPill
										tone={
											photo.visibility === "public" ? "confirmed" : "pending"
										}
										size="sm"
									>
										{PHOTO_VISIBILITY[photo.visibility]}
									</StatusPill>
									{photo.wallFeatured && (
										<StatusPill tone="boarding" size="sm">
											Mural
										</StatusPill>
									)}
									{photo.consentBasis === "pending" && (
										<StatusPill tone="muted" size="sm">
											{CONSENT_BASIS.pending}
										</StatusPill>
									)}
								</span>
							</button>
						))}
					</div>
					{photos.data?.items.length === 0 && (
						<p className={styles.empty}>Nenhuma foto aqui.</p>
					)}
					{photos.data && (
						<Pager
							page={photos.data.page}
							pageSize={photos.data.pageSize}
							total={photos.data.total}
							onPage={setPage}
						/>
					)}
				</div>
				{selected && (
					<PhotoEditor id={selected} onClose={() => setSelected(null)} />
				)}
			</div>
		</div>
	);
}
