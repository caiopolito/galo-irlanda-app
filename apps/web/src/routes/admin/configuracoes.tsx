import type { Schemas } from "@galo/api-client";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { type FormEvent, useState } from "react";
import { AdminButton } from "#/features/admin/ui/AdminButton";
import styles from "#/features/admin/ui/admin.module.css";
import { Field, inputClass } from "#/features/admin/ui/Field";
import { Notice } from "#/features/admin/ui/Notice";
import { PageHeader } from "#/features/admin/ui/PageHeader";
import { PhotoPicker } from "#/features/admin/ui/PhotoPicker";
import { ApiError, api, unwrap } from "#/lib/api";

export const Route = createFileRoute("/admin/configuracoes")({
	component: Settings,
});

function Settings() {
	const settings = useQuery({
		queryKey: ["settings"],
		queryFn: () => unwrap(api.GET("/api/admin/settings")),
	});

	return (
		<div className={styles.page}>
			<PageHeader
				title="Configurações"
				subtitle="Foto da home, mensalidade e links que aparecem no site."
			/>
			{settings.data && (
				<SettingsForm key={settings.data.version} settings={settings.data} />
			)}
		</div>
	);
}

function SettingsForm({
	settings,
}: {
	settings: Schemas["GetSiteSettingsResponse"];
}) {
	const queryClient = useQueryClient();
	const [values, setValues] = useState({
		heroPhotoId: settings.heroPhotoId,
		fee:
			settings.membershipFeeCents === null
				? ""
				: (settings.membershipFeeCents / 100).toFixed(2),
		whatsappUrl: settings.whatsappUrl ?? "",
		instagramUrl: settings.instagramUrl ?? "",
		contactEmail: settings.contactEmail ?? "",
	});
	const set = <K extends keyof typeof values>(
		key: K,
		value: (typeof values)[K],
	) => setValues((v) => ({ ...v, [key]: value }));
	const nullable = (value: string) =>
		value.trim() === "" ? null : value.trim();

	const save = useMutation({
		mutationFn: () =>
			unwrap(
				api.PUT("/api/admin/settings", {
					body: {
						heroPhotoId: values.heroPhotoId,
						membershipFeeCents:
							values.fee === ""
								? null
								: Math.round(Number(values.fee.replace(",", ".")) * 100),
						whatsappUrl: nullable(values.whatsappUrl),
						instagramUrl: nullable(values.instagramUrl),
						contactEmail: nullable(values.contactEmail),
						version: settings.version,
					},
				}),
			),
		onSuccess: () => queryClient.invalidateQueries({ queryKey: ["settings"] }),
	});
	const error = save.error instanceof ApiError ? save.error : null;

	const submit = (event: FormEvent) => {
		event.preventDefault();
		save.mutate();
	};

	return (
		<form className={styles.page} onSubmit={submit}>
			{error && <Notice tone="error">{error.summary}</Notice>}
			{save.isSuccess && <Notice tone="success">Configurações salvas.</Notice>}
			<section className={styles.card}>
				<div className={styles.form}>
					<Field
						label="Foto da home"
						full
						error={error?.field("heroPhotoId")}
						hint="A foto grande ao lado de “O Galo nunca joga sozinho.”"
					>
						{() => (
							<PhotoPicker
								value={values.heroPhotoId}
								onChange={(id) => set("heroPhotoId", id)}
							/>
						)}
					</Field>
					<Field
						label="Mensalidade (€)"
						error={error?.field("membershipFeeCents")}
						hint="Vazio mostra “€[valor]”."
					>
						{(id, invalid) => (
							<input
								id={id}
								inputMode="decimal"
								className={inputClass(invalid)}
								value={values.fee}
								onChange={(e) => set("fee", e.target.value)}
							/>
						)}
					</Field>
					<Field label="E-mail de contato" error={error?.field("contactEmail")}>
						{(id, invalid) => (
							<input
								id={id}
								type="email"
								className={inputClass(invalid)}
								value={values.contactEmail}
								onChange={(e) => set("contactEmail", e.target.value)}
							/>
						)}
					</Field>
					<Field
						label="Grupo do WhatsApp"
						error={error?.field("whatsappUrl")}
						hint="Link de convite: https://chat.whatsapp.com/…"
					>
						{(id, invalid) => (
							<input
								id={id}
								type="url"
								className={inputClass(invalid)}
								value={values.whatsappUrl}
								onChange={(e) => set("whatsappUrl", e.target.value)}
							/>
						)}
					</Field>
					<Field label="Instagram" error={error?.field("instagramUrl")}>
						{(id, invalid) => (
							<input
								id={id}
								type="url"
								className={inputClass(invalid)}
								value={values.instagramUrl}
								onChange={(e) => set("instagramUrl", e.target.value)}
							/>
						)}
					</Field>
				</div>
			</section>
			<div className={styles.actions}>
				<AdminButton type="submit" variant="primary" busy={save.isPending}>
					Salvar configurações
				</AdminButton>
			</div>
		</form>
	);
}
