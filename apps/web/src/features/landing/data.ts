/*
 * DADOS DE EXEMPLO da landing, no formato que a API vai devolver (fase 0).
 * Datas e horários são fictícios; textos entre colchetes são placeholders (ver DESIGN.md, "Não inventar dados").
 */

import type { Picture } from "vite-imagetools";
import churrasco from "#/assets/photos/galo-irlanda-1.jpg?photo";
import pubDividido from "#/assets/photos/galo-irlanda-2.jpg?photo";
import faixaNaRua from "#/assets/photos/galo-irlanda-3.jpg?photo";
import mesaDoPub from "#/assets/photos/galo-irlanda-4.webp?photo";
import turmaNoQuintal from "#/assets/photos/galo-irlanda-5.jpg?photo";
import bandeiraNaSinuca from "#/assets/photos/galo-irlanda-6.webp?photo";

export type CityCode = "DUB" | "ORK" | "GWY";

export const CITY_NAMES: Record<CityCode, string> = {
	DUB: "Dublin",
	ORK: "Cork",
	GWY: "Galway",
};

export type EventStatus =
	| { kind: "boarding" }
	| { kind: "confirmed" }
	| { kind: "spots"; taken: number; total: number }
	| { kind: "voting" };

export interface BoardEvent {
	id: string;
	kind: "match" | "meetup";
	title: string;
	/** Instante em UTC (ISO 8601). */
	startsAt: string;
	venue: string;
	city: CityCode;
	status: EventStatus;
}

export const boardEvents: BoardEvent[] = [
	{
		id: "jogo-1",
		kind: "match",
		title: "Galo × [Adversário]",
		startsAt: "2026-11-07T00:30:00Z",
		venue: "[Pub parceiro]",
		city: "DUB",
		status: { kind: "boarding" },
	},
	{
		id: "pelada-1",
		kind: "meetup",
		title: "Pelada da Massa",
		startsAt: "2026-11-08T11:00:00Z",
		venue: "Phoenix Park",
		city: "DUB",
		status: { kind: "spots", taken: 14, total: 20 },
	},
	{
		id: "jogo-2",
		kind: "match",
		title: "Galo × [Adversário]",
		startsAt: "2026-11-14T00:30:00Z",
		venue: "[Pub parceiro]",
		city: "ORK",
		status: { kind: "confirmed" },
	},
	{
		id: "churrasco-1",
		kind: "meetup",
		title: "Churrasco mineiro",
		startsAt: "2026-11-15T14:00:00Z",
		venue: "[Local a definir]",
		city: "GWY",
		status: { kind: "voting" },
	},
];

export const nextMatch = {
	title: "Galo × [Adversário]",
	startsAt: "2026-11-07T00:30:00Z",
	venue: "[Pub parceiro], [bairro]",
	homeTrip: "[linha de ônibus noturno] ou carona da Massa",
	confirmed: "[38]",
	photo: faixaNaRua,
	photoAlt:
		"Torcedores do Galo Irlanda com faixa do Atlético e tambores na porta de um pub, à noite",
};

export const hero = {
	photo: turmaNoQuintal,
	photoAlt:
		"Turma do Galo Irlanda reunida num quintal, de camisa do Atlético, em frente à faixa da torcida",
};

export interface City {
	code: CityCode;
	blurb: string;
	photoCaption: string;
}

export const cities: City[] = [
	{
		code: "DUB",
		blurb:
			"Sede da bagunça. Todo jogo no [Pub parceiro], pelada no Phoenix Park.",
		photoCaption: "[foto: a turma de Dublin]",
	},
	{
		code: "ORK",
		blurb: "Turma pequena e barulhenta. Jogos grandes no [Pub parceiro].",
		photoCaption: "[foto: a turma de Cork]",
	},
	{
		code: "GWY",
		blurb: "Ainda é um grupinho. Bora crescer? Chama no grupo.",
		photoCaption: "[foto: a turma de Galway]",
	},
];

export interface NewsItem {
	id: string;
	category: string;
	date: string;
	title: string;
}

export const news: NewsItem[] = [
	{
		id: "pub-com-som",
		category: "Encontros",
		date: "[dd/mm]",
		title: "Achamos um pub que passa o Galo com som. Sim, com som.",
	},
	{
		id: "2013",
		category: "Memória",
		date: "[dd/mm]",
		title: "Onde você estava em 2013? A Massa de Dublin conta.",
	},
	{
		id: "polvilho",
		category: "Saudade",
		date: "[dd/mm]",
		title: "Pão de queijo em Dublin: onde comprar polvilho sem chorar.",
	},
];

export interface WallPhoto {
	picture: Picture;
	alt: string;
	title: string;
	caption: string;
}

export const wallPhotos: WallPhoto[] = [
	{
		picture: churrasco,
		alt: "Torcedores posando em frente a uma casa, sob a bandeira do Galo Irlanda",
		title: "Churrasco da Massa.",
		caption: "Sol na Irlanda, acredite.",
	},
	{
		picture: pubDividido,
		alt: "Torcedores do Galo e do Palmeiras com suas faixas num pub, ao lado de uma mesa de sinuca",
		title: "Pub dividido.",
		caption: "Palmeiras de um lado, Galo do outro.",
	},
	{
		picture: mesaDoPub,
		alt: "Grupo de atleticanos com camisas listradas em volta de uma mesa de pub",
		title: "Pint na mesa,",
		caption: "Galo na tela.",
	},
	{
		picture: bandeiraNaSinuca,
		alt: "Torcedores segurando a faixa do Galo Irlanda dentro de um pub",
		title: "Faixa aberta.",
		caption: "Sinuca do lado, jogo na TV.",
	},
];

export interface Milestone {
	year: string;
	text: string;
}

export const milestones: Milestone[] = [
	{ year: "1908", text: "Nasce o Clube Atlético Mineiro, em BH." },
	{ year: "1950", text: "Campeões do Gelo na Europa. [confirmar detalhes]" },
	{ year: "1971", text: "Primeiro campeão brasileiro." },
	{ year: "2013", text: "Libertadores. Eu acredito." },
	{ year: "2021", text: "Brasileiro e Copa do Brasil. A gente viu daqui." },
];
