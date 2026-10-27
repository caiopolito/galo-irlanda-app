import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { type FormEvent, useEffect, useRef, useState } from "react";
import { Monogram } from "#/components/ui/Monogram";
import { meQueryKey } from "#/features/admin/hooks/useMe";
import { AdminButton } from "#/features/admin/ui/AdminButton";
import styles from "#/features/admin/ui/admin.module.css";
import { Field, inputClass } from "#/features/admin/ui/Field";
import { Notice } from "#/features/admin/ui/Notice";
import { ApiError, api, unwrap } from "#/lib/api";

export const Route = createFileRoute("/entrar")({
	ssr: false,
	validateSearch: (search: Record<string, unknown>): { token?: string } =>
		typeof search.token === "string" ? { token: search.token } : {},
	head: () => ({
		meta: [
			{ title: "Entrar · Galo Irlanda" },
			{ name: "robots", content: "noindex" },
		],
	}),
	component: Login,
});

function Login() {
	const { token } = Route.useSearch();
	return (
		<main
			style={{ maxWidth: 440, margin: "0 auto", padding: "64px var(--gutter)" }}
		>
			<div className={styles.page}>
				<div className={styles.actions}>
					<Monogram size={44} />
					<h1 className={styles.pageTitle}>Entrar</h1>
				</div>
				{token ? <ConsumeToken token={token} /> : <RequestLink />}
			</div>
		</main>
	);
}

function RequestLink() {
	const [email, setEmail] = useState("");
	const request = useMutation({
		mutationFn: () =>
			unwrap(api.POST("/api/auth/magic-link", { body: { email } })),
	});
	const error = request.error instanceof ApiError ? request.error : null;

	const submit = (event: FormEvent) => {
		event.preventDefault();
		request.mutate();
	};

	if (request.isSuccess) {
		return (
			<Notice tone="success">
				{request.data.message} Confira a caixa de entrada (e o spam) de{" "}
				<strong>{email}</strong>.
			</Notice>
		);
	}

	return (
		<form className={styles.card} onSubmit={submit}>
			<p className={styles.muted} style={{ margin: 0 }}>
				Mandamos um link de acesso para o seu e-mail. Sem senha.
			</p>
			<Field label="E-mail" error={error?.field("email")}>
				{(id, invalid) => (
					<input
						id={id}
						type="email"
						required
						autoComplete="email"
						className={inputClass(invalid)}
						value={email}
						onChange={(e) => setEmail(e.target.value)}
					/>
				)}
			</Field>
			{error && !error.field("email") && (
				<Notice tone="error">{error.summary}</Notice>
			)}
			<AdminButton type="submit" variant="primary" busy={request.isPending}>
				Enviar link de acesso
			</AdminButton>
		</form>
	);
}

function ConsumeToken({ token }: { token: string }) {
	const navigate = useNavigate();
	const queryClient = useQueryClient();
	const consume = useMutation({
		mutationFn: () =>
			unwrap(api.POST("/api/auth/magic-link/consume", { body: { token } })),
		onSuccess: async () => {
			await queryClient.invalidateQueries({ queryKey: meQueryKey });
			await navigate({ to: "/admin", replace: true });
		},
	});

	// O link só vale uma vez: consome uma única vez, mesmo com o StrictMode montando duas vezes.
	const started = useRef(false);
	useEffect(() => {
		if (started.current) return;
		started.current = true;
		consume.mutate();
	}, [consume]);

	if (consume.isError) {
		const error = consume.error instanceof ApiError ? consume.error : null;
		return (
			<div className={styles.page}>
				<Notice tone="error">
					{error?.field("token") ??
						error?.summary ??
						"Não foi possível entrar."}
				</Notice>
				<a href="/entrar" className={styles.rowLink}>
					Pedir um novo link
				</a>
			</div>
		);
	}

	return <Notice>Entrando…</Notice>;
}
