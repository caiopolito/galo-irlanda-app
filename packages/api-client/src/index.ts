import createClient from "openapi-fetch";
import type { components, paths } from "./schema";

export type { components, paths };

/** Esquemas da API: `Schemas["CreateEventRequest"]`. */
export type Schemas = components["schemas"];

/** Header anti-CSRF exigido pela API em toda mutação (ADR 0006). */
export const CSRF_HEADER = "X-Galo-Csrf";

/**
 * Client tipado da API, gerado a partir do OpenAPI (`npm run api:types`).
 * Envia o cookie de sessão e o header anti-CSRF em toda requisição.
 */
export function createApiClient(baseUrl = "") {
	return createClient<paths>({
		baseUrl,
		credentials: "include",
		headers: { [CSRF_HEADER]: "1" },
	});
}

export type ApiClient = ReturnType<typeof createApiClient>;
