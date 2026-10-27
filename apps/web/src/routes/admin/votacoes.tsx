import type { Schemas } from "@galo/api-client";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { type FormEvent, useState } from "react";
import { StatusPill } from "#/components/ui/StatusPill";
import { useMe } from "#/features/admin/hooks/useMe";
import { AdminButton } from "#/features/admin/ui/AdminButton";
import styles from "#/features/admin/ui/admin.module.css";
import { Field, inputClass, textareaClass } from "#/features/admin/ui/Field";
import { formatDateTime } from "#/features/admin/ui/labels";
import { Notice } from "#/features/admin/ui/Notice";
import { PageHeader } from "#/features/admin/ui/PageHeader";
import { ApiError, api, unwrap } from "#/lib/api";
import { fromZonedInput, toZonedInput } from "#/lib/time";

export const Route = createFileRoute("/admin/votacoes")({ component: Polls });

type Poll = Schemas["ListPollsItem"];

function Polls() {
	const polls = useQuery({
		queryKey: ["polls"],
		queryFn: () => unwrap(api.GET("/api/admin/polls")),
	});
	const [editing, setEditing] = useState<Poll | "new" | null>(null);

	return (
		<div className={styles.page}>
			<PageHeader
				title="Votações"
				subtitle="Só sócios com a mensalidade em dia votam. Depois do primeiro voto, opções e sigilo travam."
				actions={
					<AdminButton variant="primary" onClick={() => setEditing("new")}>
						Nova votação
					</AdminButton>
				}
			/>
			<div className={styles.split}>
				<div className={styles.page}>
					{polls.data?.length === 0 && (
						<p className={styles.empty}>Nenhuma votação ainda.</p>
					)}
					{polls.data?.map((poll) => {
						const total = poll.options.reduce((sum, o) => sum + o.votes, 0);
						return (
							<button
								key={poll.id}
								type="button"
								className={styles.card}
								style={{
									textAlign: "left",
									cursor: "pointer",
									color: "inherit",
								}}
								onClick={() => setEditing(poll)}
							>
								<div className={styles.badges}>
									<StatusPill
										tone={poll.isPublished ? "confirmed" : "pending"}
										size="sm"
									>
										{poll.isPublished ? "Publicada" : "Rascunho"}
									</StatusPill>
									{poll.isSecret && (
										<StatusPill tone="muted" size="sm">
											Secreta
										</StatusPill>
									)}
								</div>
								<h2 className={styles.cardTitle}>{poll.title}</h2>
								<span className={styles.muted}>
									Até {formatDateTime(poll.closesAt)} · {poll.participants}{" "}
									votos
								</span>
								{poll.options.map((option) => (
									<span key={option.id} className={styles.muted}>
										{option.label}: {option.votes} (
										{total ? Math.round((option.votes / total) * 100) : 0}%)
									</span>
								))}
							</button>
						);
					})}
				</div>
				{editing && (
					<PollForm
						key={editing === "new" ? "new" : `${editing.id}-${editing.version}`}
						poll={editing === "new" ? null : editing}
						onDone={() => setEditing(null)}
					/>
				)}
			</div>
		</div>
	);
}

function PollForm({ poll, onDone }: { poll: Poll | null; onDone: () => void }) {
	const queryClient = useQueryClient();
	const me = useMe();
	const now = new Date();
	const [values, setValues] = useState({
		title: poll?.title ?? "",
		description: poll?.description ?? "",
		handNote: poll?.handNote ?? "",
		isSecret: poll?.isSecret ?? true,
		opensAt: toZonedInput(poll?.opensAt ?? now.toISOString(), "dublin"),
		closesAt: toZonedInput(
			poll?.closesAt ?? new Date(now.getTime() + 7 * 86_400_000).toISOString(),
			"dublin",
		),
		options: poll?.options.map((o) => o.label).join("\n") ?? "",
		isPublished: poll?.isPublished ?? false,
	});
	const set = <K extends keyof typeof values>(
		key: K,
		value: (typeof values)[K],
	) => setValues((v) => ({ ...v, [key]: value }));
	const refresh = () => queryClient.invalidateQueries({ queryKey: ["polls"] });

	const save = useMutation({
		mutationFn: () => {
			const body = {
				title: values.title,
				description: values.description || null,
				handNote: values.handNote || null,
				isSecret: values.isSecret,
				opensAt: fromZonedInput(values.opensAt, "dublin"),
				closesAt: fromZonedInput(values.closesAt, "dublin"),
				options: values.options
					.split("\n")
					.map((o) => o.trim())
					.filter(Boolean),
			};
			return poll
				? unwrap(
						api.PUT("/api/admin/polls/{id}", {
							params: { path: { id: poll.id } },
							body: {
								...body,
								isPublished: values.isPublished,
								version: poll.version,
							},
						}),
					)
				: unwrap(
						api.POST("/api/admin/polls", {
							body: { ...body, publish: values.isPublished },
						}),
					);
		},
		onSuccess: async () => {
			await refresh();
			onDone();
		},
	});
	const remove = useMutation({
		mutationFn: () =>
			unwrap(
				api.DELETE("/api/admin/polls/{id}", {
					params: { path: { id: poll?.id ?? "" } },
				}),
			),
		onSuccess: async () => {
			await refresh();
			onDone();
		},
	});
	const error = save.error instanceof ApiError ? save.error : null;

	const submit = (event: FormEvent) => {
		event.preventDefault();
		save.mutate();
	};

	return (
		<form className={styles.card} onSubmit={submit}>
			<div className={styles.pageHeader}>
				<h2 className={styles.cardTitle}>
					{poll ? "Editar votação" : "Nova votação"}
				</h2>
				<AdminButton variant="ghost" onClick={onDone}>
					Fechar
				</AdminButton>
			</div>
			{error && (
				<Notice tone="error">{error.field("options") ?? error.summary}</Notice>
			)}
			<Field label="Pergunta" error={error?.field("title")}>
				{(id, invalid) => (
					<input
						id={id}
						required
						className={inputClass(invalid)}
						value={values.title}
						onChange={(e) => set("title", e.target.value)}
					/>
				)}
			</Field>
			<Field label="Descrição">
				{(id) => (
					<textarea
						id={id}
						className={textareaClass(false)}
						style={{ minHeight: 80 }}
						value={values.description}
						onChange={(e) => set("description", e.target.value)}
					/>
				)}
			</Field>
			<Field label="Opções" hint="Uma por linha (2 a 10).">
				{(id) => (
					<textarea
						id={id}
						required
						className={textareaClass(false)}
						style={{ minHeight: 110 }}
						value={values.options}
						onChange={(e) => set("options", e.target.value)}
					/>
				)}
			</Field>
			<Field label="Abre em (Dublin)">
				{(id) => (
					<input
						id={id}
						type="datetime-local"
						required
						className={inputClass(false)}
						value={values.opensAt}
						onChange={(e) => set("opensAt", e.target.value)}
					/>
				)}
			</Field>
			<Field label="Fecha em (Dublin)" error={error?.field("closesAt")}>
				{(id, invalid) => (
					<input
						id={id}
						type="datetime-local"
						required
						className={inputClass(invalid)}
						value={values.closesAt}
						onChange={(e) => set("closesAt", e.target.value)}
					/>
				)}
			</Field>
			<Field
				label="Anotação à mão (opcional)"
				hint="Ex.: “se chover, é no pub mesmo”."
			>
				{(id) => (
					<input
						id={id}
						maxLength={80}
						className={inputClass(false)}
						value={values.handNote}
						onChange={(e) => set("handNote", e.target.value)}
					/>
				)}
			</Field>
			<label className={styles.checkbox}>
				<input
					type="checkbox"
					checked={values.isSecret}
					onChange={(e) => set("isSecret", e.target.checked)}
				/>
				Voto secreto (ninguém sabe quem votou em quê)
			</label>
			<label className={styles.checkbox}>
				<input
					type="checkbox"
					checked={values.isPublished}
					onChange={(e) => set("isPublished", e.target.checked)}
				/>
				Publicada para os sócios
			</label>
			<div className={styles.actions}>
				<AdminButton type="submit" variant="primary" busy={save.isPending}>
					Salvar votação
				</AdminButton>
				{poll && me.data?.roles.includes("admin") && (
					<AdminButton
						variant="danger"
						busy={remove.isPending}
						onClick={() =>
							confirm("Excluir a votação e todos os votos?") && remove.mutate()
						}
					>
						Excluir
					</AdminButton>
				)}
			</div>
		</form>
	);
}
