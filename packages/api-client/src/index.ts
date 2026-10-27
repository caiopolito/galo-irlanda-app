import createClient from "openapi-fetch";
import type { components, paths } from "./schema";

export type { components, paths };

/** Esquemas da API: `Schemas["CreateEventRequest"]`. */
export type Schemas = components["schemas"];

/** Header anti-CSRF exigido pela API em toda mutação (ADR 0006). */
export const CSRF_HEADER = "X-Galo-Csrf";

/**
 * Client tipado da API, gerado a partir do OpenAPI (`npm run api:types`).
 * Envia o header anti-CSRF e, no navegador, o cookie de sessão. No servidor (Cloudflare Workers)
 * `credentials` não é suportado e não há cookie para mandar.
 */
export function createApiClient(baseUrl = "", { withCredentials = true } = {}) {
	return createClient<paths>({
		baseUrl,
		...(withCredentials ? { credentials: "include" as const } : {}),
		headers: { [CSRF_HEADER]: "1" },
	});
}

export type ApiClient = ReturnType<typeof createApiClient>;
