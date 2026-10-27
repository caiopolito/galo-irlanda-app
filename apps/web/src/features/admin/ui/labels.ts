import type { Schemas } from "@galo/api-client";

export const EVENT_KIND: Record<Schemas["EventKind"], string> = {
	match: "Jogo",
	meetup: "Encontro",
};

export const EVENT_STATUS: Record<Schemas["EventStatus"], string> = {
	scheduled: "A confirmar",
	voting: "Em votação",
	confirmed: "Confirmado",
	postponed: "Adiado",
	cancelled: "Cancelado",
};

export const POST_STATUS: Record<Schemas["PostStatus"], string> = {
	draft: "Rascunho",
	published: "Publicada",
	archived: "Arquivada",
};

export const PHOTO_VISIBILITY: Record<Schemas["PhotoVisibility"], string> = {
	public: "Pública",
	members: "Só sócios",
	private: "Privada",
};

export const CONSENT_BASIS: Record<Schemas["ConsentBasis"], string> = {
	pending: "Pendente",
	no_identifiable_people: "Sem pessoas identificáveis",
	consent_recorded: "Consentimento registrado",
	withdrawn: "Consentimento retirado",
};

export const ROLE: Record<string, string> = {
	admin: "Admin",
	editor: "Editor",
	socio: "Sócio",
};

export function formatDateTime(iso: string) {
	return new Intl.DateTimeFormat("pt-BR", {
		timeZone: "Europe/Dublin",
		dateStyle: "short",
		timeStyle: "short",
	}).format(new Date(iso));
}
