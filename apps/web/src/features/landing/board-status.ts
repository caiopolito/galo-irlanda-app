import type { StatusTone } from "#/components/ui/StatusPill";
import type { BoardEvent } from "./types";

const BOARDING_BEFORE_MS = 6 * 60 * 60 * 1000;
const BOARDING_AFTER_MS = 3 * 60 * 60 * 1000;

export interface BoardStatus {
	tone: StatusTone;
	label: string;
	boarding: boolean;
}

/**
 * Selo do painel (docs/database/schema.md). "Embarcando" depende do relógio, por isso é calculado
 * aqui e só no navegador (`now` é null no HTML gerado no servidor).
 */
export function boardStatus(event: BoardEvent, now: Date | null): BoardStatus {
	const start = new Date(event.startsAt).getTime();
	switch (event.status) {
		case "cancelled":
			return { tone: "pending", label: "Cancelado", boarding: false };
		case "postponed":
			return { tone: "muted", label: "Adiado", boarding: false };
		case "voting":
			return { tone: "pending", label: "Em votação", boarding: false };
	}

	if (event.status === "confirmed" && now) {
		const diff = start - now.getTime();
		if (diff <= BOARDING_BEFORE_MS && diff >= -BOARDING_AFTER_MS) {
			return { tone: "boarding", label: "Embarcando", boarding: true };
		}
	}
	if (event.capacity !== null) {
		return {
			tone: "muted",
			label: `${event.spotsTaken} de ${event.capacity} vagas`,
			boarding: false,
		};
	}
	return event.status === "confirmed"
		? { tone: "confirmed", label: "Confirmado", boarding: false }
		: { tone: "muted", label: "A confirmar", boarding: false };
}
