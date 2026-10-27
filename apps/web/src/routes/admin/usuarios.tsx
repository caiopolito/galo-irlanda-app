import type { Schemas } from "@galo/api-client";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { type FormEvent, useState } from "react";
import { StatusPill } from "#/components/ui/StatusPill";
import { useMe } from "#/features/admin/hooks/useMe";
import { AdminButton } from "#/features/admin/ui/AdminButton";
import styles from "#/features/admin/ui/admin.module.css";
import { Field, inputClass, selectClass } from "#/features/admin/ui/Field";
import { formatDateTime, ROLE } from "#/features/admin/ui/labels";
import { Notice } from "#/features/admin/ui/Notice";
import { PageHeader } from "#/features/admin/ui/PageHeader";
import { ApiError, api, unwrap } from "#/lib/api";

export const Route = createFileRoute("/admin/usuarios")({ component: Users });

function Users() {
	const users = useQuery({
		queryKey: ["users"],
		queryFn: () => unwrap(api.GET("/api/admin/users")),
	});

	return (
		<div className={styles.page}>
			<PageHeader
				title="Usuários"
				subtitle="Quem pode editar o site. Editores cuidam do conteúdo; admins também gerenciam pessoas."
			/>
			{users.isError && (
				<Notice tone="error">Só admins veem esta página.</Notice>
			)}
			<div className={styles.tableWrap}>
				<table className={styles.table}>
					<thead>
						<tr>
							<th scope="col">Pessoa</th>
							<th scope="col">Papéis</th>
							<th scope="col">Último acesso</th>
							<th scope="col" />
						</tr>
					</thead>
					<tbody>
						{users.data?.map((user) => (
							<UserRow key={user.id} user={user} />
						))}
					</tbody>
				</table>
			</div>
			<InviteForm />
		</div>
	);
}

function UserRow({ user }: { user: Schemas["ListUsersItem"] }) {
	const queryClient = useQueryClient();
	const me = useMe();
	const refresh = () => queryClient.invalidateQueries({ queryKey: ["users"] });
	const path = { params: { path: { id: user.id } } };

	const roles = useMutation({
		mutationFn: (next: string[]) =>
			unwrap(
				api.PUT("/api/admin/users/{id}/roles", {
					...path,
					body: { roles: next },
				}),
			),
		onSuccess: refresh,
	});
	const status = useMutation({
		mutationFn: (disable: boolean) =>
			unwrap(
				disable
					? api.POST("/api/admin/users/{id}/disable", path)
					: api.POST("/api/admin/users/{id}/enable", path),
			),
		onSuccess: refresh,
	});
	const toggleRole = (role: string) =>
		roles.mutate(
			user.roles.includes(role)
				? user.roles.filter((r) => r !== role)
				: [...user.roles, role],
		);
	const error = [roles.error, status.error].find(
		(e) => e instanceof ApiError,
	) as ApiError | undefined;
	const disabled = user.status === "disabled";
	const isMe = me.data?.id === user.id;

	return (
		<tr>
			<td>
				<strong>{user.displayName}</strong>
				{isMe && <span className={styles.muted}> (você)</span>}
				<div className={styles.muted}>{user.email}</div>
				{error && (
					<div className={styles.error}>
						{error.field("roles") ?? error.field("id") ?? error.summary}
					</div>
				)}
			</td>
			<td>
				<div className={styles.segmented}>
					{["admin", "editor"].map((role) => (
						<AdminButton
							key={role}
							selected={user.roles.includes(role)}
							busy={roles.isPending}
							onClick={() => toggleRole(role)}
						>
							{user.roles.includes(role) ? "✓ " : ""}
							{ROLE[role]}
						</AdminButton>
					))}
				</div>
			</td>
			<td className={styles.muted}>
				{user.lastLoginAt ? formatDateTime(user.lastLoginAt) : "Nunca entrou"}
			</td>
			<td>
				{disabled && (
					<StatusPill tone="pending" size="sm">
						Desativado
					</StatusPill>
				)}{" "}
				{!isMe && (
					<AdminButton
						variant={disabled ? "secondary" : "danger"}
						busy={status.isPending}
						onClick={() => status.mutate(!disabled)}
					>
						{disabled ? "Reativar" : "Desativar"}
					</AdminButton>
				)}
			</td>
		</tr>
	);
}

function InviteForm() {
	const queryClient = useQueryClient();
	const [values, setValues] = useState({
		email: "",
		displayName: "",
		role: "editor",
	});
	const invite = useMutation({
		mutationFn: () =>
			unwrap(api.POST("/api/admin/users/invite", { body: values })),
		onSuccess: async () => {
			setValues({ email: "", displayName: "", role: "editor" });
			await queryClient.invalidateQueries({ queryKey: ["users"] });
		},
	});
	const error = invite.error instanceof ApiError ? invite.error : null;

	const submit = (event: FormEvent) => {
		event.preventDefault();
		invite.mutate();
	};

	return (
		<form className={styles.card} onSubmit={submit}>
			<h2 className={styles.cardTitle}>Convidar voluntário</h2>
			<p className={styles.muted} style={{ margin: 0 }}>
				A pessoa recebe um e-mail com o endereço de entrada e acessa pelo link
				mágico.
			</p>
			{error && <Notice tone="error">{error.summary}</Notice>}
			{invite.isSuccess && <Notice tone="success">Convite enviado.</Notice>}
			<div className={styles.form}>
				<Field label="Nome" error={error?.field("displayName")}>
					{(id, invalid) => (
						<input
							id={id}
							required
							className={inputClass(invalid)}
							value={values.displayName}
							onChange={(e) =>
								setValues({ ...values, displayName: e.target.value })
							}
						/>
					)}
				</Field>
				<Field label="E-mail" error={error?.field("email")}>
					{(id, invalid) => (
						<input
							id={id}
							type="email"
							required
							className={inputClass(invalid)}
							value={values.email}
							onChange={(e) => setValues({ ...values, email: e.target.value })}
						/>
					)}
				</Field>
				<Field label="Papel">
					{(id) => (
						<select
							id={id}
							className={selectClass(false)}
							value={values.role}
							onChange={(e) => setValues({ ...values, role: e.target.value })}
						>
							<option value="editor">Editor</option>
							<option value="admin">Admin</option>
						</select>
					)}
				</Field>
			</div>
			<div className={styles.actions}>
				<AdminButton type="submit" variant="primary" busy={invite.isPending}>
					Enviar convite
				</AdminButton>
			</div>
		</form>
	);
}
