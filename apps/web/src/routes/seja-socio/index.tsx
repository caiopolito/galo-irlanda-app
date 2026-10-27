import { useMutation } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { type FormEvent, useState } from "react";
import { SiteFooter } from "#/components/layout/SiteFooter";
import { SiteHeader } from "#/components/layout/SiteHeader";
import { Clover } from "#/components/ui/Clover";
import { HandNote } from "#/components/ui/HandNote";
import { externalLinks } from "#/config/links";
import { AdminButton } from "#/features/admin/ui/AdminButton";
import admin from "#/features/admin/ui/admin.module.css";
import { Field, inputClass, selectClass } from "#/features/admin/ui/Field";
import { Notice } from "#/features/admin/ui/Notice";
import styles from "#/features/member/signup.module.css";
import { ApiError, api, unwrap } from "#/lib/api";
import { formatMoney } from "#/lib/money";

interface Search {
	ref?: string;
	cancelado?: boolean;
}

export const Route = createFileRoute("/seja-socio/")({
	validateSearch: (search: Record<string, unknown>): Search => ({
		...(typeof search.ref === "string" ? { ref: search.ref } : {}),
		...(search.cancelado ? { cancelado: true } : {}),
	}),
	loader: () => unwrap(api.GET("/api/public/landing")),
	head: () => ({
		meta: [
			{ title: "Seja sócio · Galo Irlanda" },
			{
				name: "description",
				content: "Vire sócio da torcida de atleticanos na Irlanda.",
			},
		],
	}),
	component: BecomeMember,
});

function BecomeMember() {
	const content = Route.useLoaderData();
	const { ref, cancelado } = Route.useSearch();
	const [values, setValues] = useState({
		fullName: "",
		email: "",
		cityCode: "DUB",
		phone: "",
	});
	const set = (key: keyof typeof values, value: string) =>
		setValues((v) => ({ ...v, [key]: value }));

	const checkout = useMutation({
		mutationFn: () =>
			unwrap(
				api.POST("/api/public/membership/checkout", {
					body: {
						...values,
						phone: values.phone || null,
						referralCode: ref ?? null,
					},
				}),
			),
		onSuccess: ({ checkoutUrl }) => {
			window.location.assign(checkoutUrl);
		},
	});
	const error = checkout.error instanceof ApiError ? checkout.error : null;
	const fee = content.settings.membershipFeeCents;

	const submit = (event: FormEvent) => {
		event.preventDefault();
		checkout.mutate();
	};

	return (
		<>
			<SiteHeader external={externalLinks(content.settings)} />
			<main className={styles.page}>
				<section className={styles.intro}>
					<HandNote>vem com a gente</HandNote>
					<h1 className={styles.title}>Seja sócio da Massa.</h1>
					<p className={styles.lead}>
						Pagou, embarcou: a carteirinha chega na hora, com número de sócio e
						QR code para mostrar no pub parceiro.
					</p>
					<ul className={styles.perks}>
						{[
							"Desconto nos pubs parceiros em dia de jogo",
							"Lugar guardado em jogo lotado",
							"Voto nas decisões da torcida",
							"A carteirinha mais bonita da Irlanda",
						].map((perk) => (
							<li key={perk}>
								<Clover
									filled
									size={16}
									style={{ color: "var(--color-green)" }}
								/>
								{perk}
							</li>
						))}
					</ul>
					<span className={styles.price}>
						{fee === null
							? "€[valor]"
							: formatMoney(fee, content.settings.membershipCurrency)}
						<small>/mês · cancele quando quiser</small>
					</span>
				</section>

				<form className={admin.card} onSubmit={submit}>
					{cancelado && (
						<Notice>
							Pagamento cancelado. Seus dados não foram salvos; é só tentar de
							novo.
						</Notice>
					)}
					{ref && (
						<Notice tone="success">
							Você veio por indicação de um sócio. Bem-vindo!
						</Notice>
					)}
					<Field label="Nome completo" error={error?.field("fullName")}>
						{(id, invalid) => (
							<input
								id={id}
								required
								autoComplete="name"
								className={inputClass(invalid)}
								value={values.fullName}
								onChange={(e) => set("fullName", e.target.value)}
							/>
						)}
					</Field>
					<Field
						label="E-mail"
						error={error?.field("email")}
						hint="É por ele que você entra na área do sócio."
					>
						{(id, invalid) => (
							<input
								id={id}
								type="email"
								required
								autoComplete="email"
								className={inputClass(invalid)}
								value={values.email}
								onChange={(e) => set("email", e.target.value)}
							/>
						)}
					</Field>
					<Field label="Cidade" error={error?.field("cityCode")}>
						{(id, invalid) => (
							<select
								id={id}
								className={selectClass(invalid)}
								value={values.cityCode}
								onChange={(e) => set("cityCode", e.target.value)}
							>
								{content.cities.map((city) => (
									<option key={city.code} value={city.code.trim()}>
										{city.name}
									</option>
								))}
							</select>
						)}
					</Field>
					<Field
						label="WhatsApp (opcional)"
						hint="Para a diretoria falar com você."
					>
						{(id) => (
							<input
								id={id}
								type="tel"
								autoComplete="tel"
								className={inputClass(false)}
								value={values.phone}
								onChange={(e) => set("phone", e.target.value)}
							/>
						)}
					</Field>
					{error && !error.problem.errors && (
						<Notice tone="error">{error.summary}</Notice>
					)}
					<AdminButton
						type="submit"
						variant="primary"
						busy={checkout.isPending}
					>
						Ir para o pagamento
					</AdminButton>
					<span className={admin.hint}>
						Pagamento seguro pelo Stripe. A mensalidade renova todo mês.
					</span>
				</form>
			</main>
			<SiteFooter external={externalLinks(content.settings)} />
		</>
	);
}
