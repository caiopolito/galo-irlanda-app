import type { Schemas } from "@galo/api-client";

/** Links fixos do site. Os externos vêm das Configurações do admin; sem valor, ficam âncoras provisórias. */
export const links = {
	memberArea: "/socio",
	becomeMember: "/seja-socio",
	allNews: "#todas-as-noticias",
	partnerPubs: "#cidades",
};

export interface ExternalLinks {
	whatsappGroup: string;
	instagram: string;
	contact: string;
}

export function externalLinks(
	settings?: Schemas["GetLandingSiteLinks"],
): ExternalLinks {
	return {
		whatsappGroup: settings?.whatsappUrl ?? "#entrar-no-grupo",
		instagram: settings?.instagramUrl ?? "#instagram",
		contact: settings?.contactEmail
			? `mailto:${settings.contactEmail}`
			: "#contato",
	};
}
