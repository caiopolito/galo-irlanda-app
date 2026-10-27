import { createApiClient } from "@galo/api-client";

const isServer = typeof window === "undefined";

/**
 * Endereço da API. Produção: `VITE_API_URL`. Desenvolvimento: no navegador, "/api" passa pelo proxy
 * do Vite; no servidor (renderização da landing), a API é chamada direto.
 */
export const apiBaseUrl =
	import.meta.env.VITE_API_URL ?? (isServer ? "http://localhost:5137" : "");

export const api = createApiClient(apiBaseUrl, { withCredentials: !isServer });

export interface Problem {
	title?: string;
	errors?: Record<string, string[]>;
}

/** Erro de chamada à API, com os erros de validação por campo (ProblemDetails). */
export class ApiError extends Error {
	constructor(
		readonly status: number,
		readonly problem: Problem = {},
	) {
		super(problem.title ?? `Erro ${status}`);
	}

	/** Primeira mensagem de um campo (nome em camelCase, como vem da API). */
	field(name: string) {
		return this.problem.errors?.[name]?.[0];
	}

	get summary() {
		if (this.status === 409)
			return (
				this.problem.title ??
				"Alguém alterou isto antes. Recarregue e tente de novo."
			);
		if (this.status === 403) return "Você não tem permissão para isso.";
		if (this.problem.errors) return "Confira os campos destacados.";
		return this.problem.title ?? "Algo deu errado. Tente de novo.";
	}
}

/** Desembrulha a resposta do openapi-fetch: devolve `data` ou lança `ApiError`. */
export async function unwrap<T>(
	request: Promise<{ data?: T; error?: unknown; response: Response }>,
): Promise<T> {
	const { data, error, response } = await request;
	if (!response.ok)
		throw new ApiError(response.status, (error ?? {}) as Problem);
	return data as T;
}

/** Upload de arquivo (multipart), fora do client tipado. */
export async function uploadPhoto(file: File) {
	const body = new FormData();
	body.append("file", file);
	const response = await fetch(
		`${import.meta.env.VITE_API_URL ?? ""}/api/admin/photos`,
		{
			method: "POST",
			body,
			credentials: "include",
			headers: { "X-Galo-Csrf": "1" },
		},
	);
	if (!response.ok)
		throw new ApiError(
			response.status,
			await response.json().catch(() => ({})),
		);
	return (await response.json()) as {
		id: string;
		baseUrl: string;
		width: number;
		height: number;
		version: number;
	};
}

/** URL de uma variante de foto (480, 1024 ou 2048 px). */
export function photoUrl(baseUrl: string, width: 480 | 1024 | 2048 = 480) {
	return `${baseUrl}${width}.webp`;
}
