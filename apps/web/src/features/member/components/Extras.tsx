import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api, apiBaseUrl, photoUrl, unwrap } from "#/lib/api";
import styles from "../member.module.css";
import { memberKeys } from "../useMember";

export function Benefits() {
	const benefits = useQuery({
		queryKey: memberKeys.benefits,
		queryFn: () => unwrap(api.GET("/api/socio/benefits")),
	});
	if (!benefits.data?.length) return null;

	return (
		<section
			id="beneficios"
			style={{ display: "flex", flexDirection: "column", gap: 16 }}
		>
			<h2 className={styles.h2}>Seus benefícios</h2>
			<div className={styles.benefits}>
				{benefits.data.map((benefit) => (
					<div key={benefit.id} className={styles.benefit}>
						<span
							className={`${styles.benefitHeadline} ${benefit.highlight ? styles.benefitHighlight : ""}`}
						>
							{benefit.headline}
						</span>
						<strong>{benefit.title}</strong>
						{benefit.description && (
							<span className={styles.muted} style={{ fontSize: 14 }}>
								{benefit.description}
							</span>
						)}
					</div>
				))}
			</div>
		</section>
	);
}

export function TaggedPhotos() {
	const queryClient = useQueryClient();
	const photos = useQuery({
		queryKey: memberKeys.photos,
		queryFn: () => unwrap(api.GET("/api/socio/photos")),
	});
	const untag = useMutation({
		mutationFn: (photoId: string) =>
			unwrap(
				api.DELETE("/api/socio/photos/{photoId}/tag", {
					params: { path: { photoId } },
				}),
			),
		onSuccess: () =>
			queryClient.invalidateQueries({ queryKey: memberKeys.photos }),
	});

	return (
		<section
			style={{ display: "flex", flexDirection: "column", gap: 14, minWidth: 0 }}
		>
			<div className={styles.sectionHead}>
				<h2 className={styles.h2}>Fotos em que você aparece</h2>
				{photos.data && (
					<span className={styles.muted}>{photos.data.total} no total</span>
				)}
			</div>
			{photos.data?.total === 0 ? (
				<p className={styles.text}>
					Nenhuma ainda. Quando a diretoria marcar você numa foto, ela aparece
					aqui. Não quer ser marcado? Desligue no perfil.
				</p>
			) : (
				<div className={styles.photos}>
					{photos.data?.photos.map((photo) => (
						<div key={photo.id} className={styles.photoItem}>
							<img
								src={photoUrl(photo.baseUrl)}
								alt={photo.altText ?? ""}
								loading="lazy"
							/>
							<button
								type="button"
								className={styles.untag}
								onClick={() => untag.mutate(photo.id)}
							>
								Tirar minha marcação
							</button>
						</div>
					))}
				</div>
			)}
		</section>
	);
}

export function Documents() {
	const documents = useQuery({
		queryKey: memberKeys.documents,
		queryFn: () => unwrap(api.GET("/api/socio/documents")),
	});

	return (
		<section className={styles.panel}>
			<h2 className={styles.h2}>Documentos</h2>
			{documents.data?.length === 0 ? (
				<p className={styles.text}>Nenhum documento publicado ainda.</p>
			) : (
				<ul className={styles.docs}>
					{documents.data?.map((doc) => (
						<li key={doc.id}>
							<a href={`${apiBaseUrl}/api/socio/documents/${doc.id}/download`}>
								<span>{doc.title}</span>
								<span aria-hidden="true">↓</span>
							</a>
						</li>
					))}
				</ul>
			)}
		</section>
	);
}

export function Invite({
	referralCode,
	referrals,
}: {
	referralCode: string;
	referrals: number;
}) {
	const origin = typeof window !== "undefined" ? window.location.origin : "";
	const message = `Bora ser sócio do Galo Irlanda? A torcida de atleticanos na Irlanda. ${origin}/seja-socio?ref=${referralCode}`;

	return (
		<section className={styles.invite}>
			<div
				style={{
					display: "flex",
					flexDirection: "column",
					gap: 6,
					maxWidth: 620,
				}}
			>
				<h2 className={styles.h2}>
					Conhece outro atleticano perdido na Irlanda?
				</h2>
				<p className={styles.text}>
					Mande o convite. Se ele virar sócio, a primeira pint é por conta da
					Massa.
					{referrals > 0 &&
						` Você já trouxe ${referrals} ${referrals === 1 ? "sócio" : "sócios"}!`}
				</p>
			</div>
			<a
				href={`https://wa.me/?text=${encodeURIComponent(message)}`}
				target="_blank"
				rel="noreferrer"
				className={`${styles.btn} ${styles.btnAmber}`}
			>
				Mandar convite no WhatsApp
			</a>
		</section>
	);
}
