import type { Schemas } from "@galo/api-client";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { type FormEvent, useState } from "react";
import { AdminButton } from "#/features/admin/ui/AdminButton";
import admin from "#/features/admin/ui/admin.module.css";
import { Field, inputClass, selectClass } from "#/features/admin/ui/Field";
import { Notice } from "#/features/admin/ui/Notice";
import styles from "#/features/member/member.module.css";
import { useMemberProfile } from "#/features/member/useMember";
import { ApiError, api, unwrap } from "#/lib/api";

export const Route = createFileRoute("/socio/perfil")({ component: Profile });

function Profile() {
	const me = useMemberProfile();
	return (
		<main className={styles.main} style={{ maxWidth: 640 }}>
			<h1 className={styles.h2Big}>Seu perfil</h1>
			{me.data && <ProfileForm key={me.data.version} me={me.data} />}
			<Sessions />
		</main>
	);
}

function ProfileForm({ me }: { me: Schemas["GetMemberDashboardResponse"] }) {
	const queryClient = useQueryClient();
	const landing = useQuery({
		queryKey: ["landing"],
		queryFn: () => unwrap(api.GET("/api/public/landing")),
	});
	const [values, setValues] = useState({
		fullName: me.fullName,
		phone: me.phone ?? "",
		cityCode: me.cityCode,
		photoTagOptIn: me.photoTagOptIn,
	});
	const save = useMutation({
		mutationFn: () =>
			unwrap(
				api.PUT("/api/socio/me", {
					body: { ...values, phone: values.phone || null, version: me.version },
				}),
			),
		onSuccess: () => queryClient.invalidateQueries({ queryKey: ["socio"] }),
	});
	const error = save.error instanceof ApiError ? save.error : null;

	const submit = (event: FormEvent) => {
		event.preventDefault();
		if (
			!values.photoTagOptIn &&
			me.photoTagOptIn &&
			!confirm("Desligar a marcação remove você de todas as fotos. Continuar?")
		)
			return;
		save.mutate();
	};

	return (
		<form className={admin.card} onSubmit={submit}>
			{error && <Notice tone="error">{error.summary}</Notice>}
			{save.isSuccess && <Notice tone="success">Perfil salvo.</Notice>}
			<p className={admin.muted} style={{ margin: 0 }}>
				E-mail de acesso: <strong>{me.email}</strong>
			</p>
			<Field
				label="Nome completo"
				error={error?.field("fullName")}
				hint="É o nome que aparece na carteirinha."
			>
				{(id, invalid) => (
					<input
						id={id}
						required
						className={inputClass(invalid)}
						value={values.fullName}
						onChange={(e) => setValues({ ...values, fullName: e.target.value })}
					/>
				)}
			</Field>
			<Field label="WhatsApp">
				{(id) => (
					<input
						id={id}
						type="tel"
						className={inputClass(false)}
						value={values.phone}
						onChange={(e) => setValues({ ...values, phone: e.target.value })}
					/>
				)}
			</Field>
			<Field label="Cidade">
				{(id) => (
					<select
						id={id}
						className={selectClass(false)}
						value={values.cityCode}
						onChange={(e) => setValues({ ...values, cityCode: e.target.value })}
					>
						{landing.data?.cities.map((city) => (
							<option key={city.code} value={city.code.trim()}>
								{city.name}
							</option>
						))}
					</select>
				)}
			</Field>
			<label className={admin.checkbox}>
				<input
					type="checkbox"
					checked={values.photoTagOptIn}
					onChange={(e) =>
						setValues({ ...values, photoTagOptIn: e.target.checked })
					}
				/>
				Pode me marcar nas fotos da torcida
			</label>
			<AdminButton type="submit" variant="primary" busy={save.isPending}>
				Salvar perfil
			</AdminButton>
		</form>
	);
}

function Sessions() {
	const navigate = useNavigate();
	const queryClient = useQueryClient();
	const leave = useMutation({
		mutationFn: (everywhere: boolean) =>
			unwrap(
				everywhere
					? api.POST("/api/auth/logout-everywhere")
					: api.POST("/api/auth/logout"),
			),
		onSuccess: async () => {
			queryClient.clear();
			await navigate({ to: "/" });
		},
	});

	return (
		<section className={admin.card}>
			<h2 className={admin.cardTitle}>Sessão</h2>
			<div className={admin.actions}>
				<AdminButton onClick={() => leave.mutate(false)}>Sair</AdminButton>
				<AdminButton variant="ghost" onClick={() => leave.mutate(true)}>
					Sair de todos os aparelhos
				</AdminButton>
			</div>
		</section>
	);
}
