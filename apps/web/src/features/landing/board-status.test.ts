import { describe, expect, it } from "vitest";
import { boardStatus } from "./board-status";
import type { BoardEvent } from "./types";

const event = (overrides: Partial<BoardEvent> = {}): BoardEvent => ({
	id: "1",
	slug: "jogo",
	kind: "match",
	title: "Galo × Adversário",
	startsAt: "2026-11-07T00:30:00Z",
	place: "Pub",
	cityCode: "DUB",
	cityName: "Dublin",
	status: "confirmed",
	capacity: null,
	spotsTaken: 0,
	...overrides,
});

describe("boardStatus", () => {
	it("is boarding from 6 hours before kickoff until 3 hours after", () => {
		expect(boardStatus(event(), new Date("2026-11-06T19:00:00Z")).label).toBe(
			"Embarcando",
		);
		expect(boardStatus(event(), new Date("2026-11-07T03:00:00Z")).label).toBe(
			"Embarcando",
		);
		expect(boardStatus(event(), new Date("2026-11-06T18:00:00Z")).label).toBe(
			"Confirmado",
		);
	});

	it("never shows boarding without a clock (server-rendered HTML)", () => {
		expect(boardStatus(event(), null).boarding).toBe(false);
	});

	it("shows spots when there is a capacity", () => {
		expect(
			boardStatus(event({ capacity: 20, spotsTaken: 14 }), null).label,
		).toBe("14 de 20 vagas");
	});

	it("voting and scheduled events have their own labels", () => {
		expect(boardStatus(event({ status: "voting" }), null).label).toBe(
			"Em votação",
		);
		expect(boardStatus(event({ status: "scheduled" }), null).label).toBe(
			"A confirmar",
		);
	});
});
