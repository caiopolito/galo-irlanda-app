import { useQuery } from "@tanstack/react-query";
import { useRef } from "react";
import { api, photoUrl, unwrap } from "#/lib/api";
import { cx } from "#/lib/cx";
import { AdminButton } from "./AdminButton";
import styles from "./admin.module.css";

interface PhotoPickerProps {
	value: string | null;
	onChange: (photoId: string | null) => void;
	/** Só fotos públicas podem ir para o site. */
	publicOnly?: boolean;
}

/** Escolhe uma foto já enviada (em Fotos). Mostra a miniatura da escolhida. */
export function PhotoPicker({
	value,
	onChange,
	publicOnly = true,
}: PhotoPickerProps) {
	const dialog = useRef<HTMLDialogElement>(null);
	const photos = useQuery({
		queryKey: ["photos", "picker"],
		queryFn: () =>
			unwrap(
				api.GET("/api/admin/photos", { params: { query: { PageSize: 100 } } }),
			),
	});
	const options = (photos.data?.items ?? []).filter(
		(p) => !publicOnly || p.visibility === "public",
	);
	const selected = photos.data?.items.find((p) => p.id === value);

	return (
		<div className={styles.actions}>
			{selected ? (
				<img
					src={photoUrl(selected.baseUrl)}
					alt=""
					style={{ width: 72 }}
					className={styles.thumb}
				/>
			) : (
				<span className={styles.muted}>
					{value ? "Foto selecionada" : "Nenhuma foto"}
				</span>
			)}
			<AdminButton onClick={() => dialog.current?.showModal()}>
				Escolher foto
			</AdminButton>
			{value && (
				<AdminButton variant="ghost" onClick={() => onChange(null)}>
					Remover
				</AdminButton>
			)}

			<dialog ref={dialog} className={styles.dialog}>
				<div className={styles.page}>
					<div className={styles.pageHeader}>
						<h2 className={styles.cardTitle}>Escolher foto</h2>
						<AdminButton
							variant="ghost"
							onClick={() => dialog.current?.close()}
						>
							Fechar
						</AdminButton>
					</div>
					{publicOnly && (
						<p className={styles.hint}>
							Só aparecem fotos públicas (com consentimento). Publique uma foto
							em Fotos.
						</p>
					)}
					<div className={styles.photoGrid}>
						{options.map((photo) => (
							<button
								key={photo.id}
								type="button"
								className={cx(
									styles.photoTile,
									photo.id === value && styles.photoTileSelected,
								)}
								onClick={() => {
									onChange(photo.id);
									dialog.current?.close();
								}}
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
							</button>
						))}
					</div>
				</div>
			</dialog>
		</div>
	);
}
