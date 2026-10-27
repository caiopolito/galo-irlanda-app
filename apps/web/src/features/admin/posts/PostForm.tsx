import type { Schemas } from "@galo/api-client";
import { useQuery } from "@tanstack/react-query";
import { type FormEvent, type ReactNode, useState } from "react";
import { AdminButton } from "#/features/admin/ui/AdminButton";
import styles from "#/features/admin/ui/admin.module.css";
import {
	Field,
	inputClass,
	selectClass,
	textareaClass,
} from "#/features/admin/ui/Field";
import { Notice } from "#/features/admin/ui/Notice";
import { PhotoPicker } from "#/features/admin/ui/PhotoPicker";
import { ApiError, api, unwrap } from "#/lib/api";

export interface PostFormValues {
	title: string;
	excerpt: string;
	bodyMd: string;
	categoryId: string;
	coverPhotoId: string | null;
}

export const emptyPost: PostFormValues = {
	title: "",
	excerpt: "",
	bodyMd: "",
	categoryId: "",
	coverPhotoId: null,
};

export function postToForm(post: Schemas["GetPostResponse"]): PostFormValues {
	return {
		title: post.title,
		excerpt: post.excerpt ?? "",
		bodyMd: post.bodyMd,
		categoryId: post.categoryId?.toString() ?? "",
		coverPhotoId: post.coverPhotoId,
	};
}

export function postFormToRequest(values: PostFormValues) {
	return {
		title: values.title.trim(),
		excerpt: values.excerpt.trim() === "" ? null : values.excerpt.trim(),
		bodyMd: values.bodyMd,
		categoryId: values.categoryId === "" ? null : Number(values.categoryId),
		coverPhotoId: values.coverPhotoId,
	};
}

interface PostFormProps {
	initial: PostFormValues;
	submitLabel: string;
	saving: boolean;
	error: unknown;
	onSubmit: (values: PostFormValues) => void;
	extraActions?: ReactNode;
}

export function PostForm({
	initial,
	submitLabel,
	saving,
	error,
	onSubmit,
	extraActions,
}: PostFormProps) {
	const [values, setValues] = useState(initial);
	const set = <K extends keyof PostFormValues>(
		key: K,
		value: PostFormValues[K],
	) => setValues((current) => ({ ...current, [key]: value }));
	const apiError = error instanceof ApiError ? error : null;
	const categories = useQuery({
		queryKey: ["post-categories"],
		queryFn: () => unwrap(api.GET("/api/admin/posts/categories")),
	});

	const submit = (event: FormEvent) => {
		event.preventDefault();
		onSubmit(values);
	};

	return (
		<form className={styles.page} onSubmit={submit}>
			{apiError && <Notice tone="error">{apiError.summary}</Notice>}
			<section className={styles.card}>
				<div className={styles.form}>
					<Field label="Título" full error={apiError?.field("title")}>
						{(id, invalid) => (
							<input
								id={id}
								required
								maxLength={160}
								className={inputClass(invalid)}
								value={values.title}
								onChange={(e) => set("title", e.target.value)}
							/>
						)}
					</Field>
					<Field label="Categoria" error={apiError?.field("categoryId")}>
						{(id, invalid) => (
							<select
								id={id}
								className={selectClass(invalid)}
								value={values.categoryId}
								onChange={(e) => set("categoryId", e.target.value)}
							>
								<option value="">Sem categoria</option>
								{categories.data?.map((category) => (
									<option key={category.id} value={category.id}>
										{category.name}
									</option>
								))}
							</select>
						)}
					</Field>
					<Field label="Capa" error={apiError?.field("coverPhotoId")}>
						{() => (
							<PhotoPicker
								value={values.coverPhotoId}
								onChange={(id) => set("coverPhotoId", id)}
							/>
						)}
					</Field>
					<Field
						label="Resumo"
						full
						hint="Uma ou duas frases para a lista de notícias (até 300 caracteres)."
					>
						{(id) => (
							<input
								id={id}
								maxLength={300}
								className={inputClass(false)}
								value={values.excerpt}
								onChange={(e) => set("excerpt", e.target.value)}
							/>
						)}
					</Field>
					<Field
						label="Texto"
						full
						hint="Markdown: **negrito**, _itálico_, [link](https://…), linhas em branco separam parágrafos."
					>
						{(id) => (
							<textarea
								id={id}
								className={textareaClass(false)}
								style={{ minHeight: 320 }}
								value={values.bodyMd}
								onChange={(e) => set("bodyMd", e.target.value)}
							/>
						)}
					</Field>
				</div>
			</section>
			<div className={styles.actions}>
				<AdminButton type="submit" variant="primary" busy={saving}>
					{submitLabel}
				</AdminButton>
				{extraActions}
			</div>
		</form>
	);
}
