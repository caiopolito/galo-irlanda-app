// Horários chegam em UTC e são sempre exibidos por fuso, nunca por diferença fixa (ADR 0009).
export const TIME_ZONES = {
	dublin: "Europe/Dublin",
	bh: "America/Sao_Paulo",
} as const;

export type Zone = keyof typeof TIME_ZONES;

function format(
	date: Date,
	zone: Zone,
	options: Intl.DateTimeFormatOptions,
	locale = "pt-BR",
) {
	return new Intl.DateTimeFormat(locale, {
		timeZone: TIME_ZONES[zone],
		...options,
	}).format(date);
}

/** "00:30" */
export function formatTime(date: Date, zone: Zone) {
	return format(date, zone, {
		hour: "2-digit",
		minute: "2-digit",
		hourCycle: "h23",
	});
}

/** "sáb" */
export function formatWeekdayShort(date: Date, zone: Zone) {
	return format(date, zone, { weekday: "short" }).replace(".", "");
}

/** "Sábado" */
export function formatWeekdayLong(date: Date, zone: Zone) {
	const weekday = format(date, zone, { weekday: "long" }).split("-")[0];
	return weekday.charAt(0).toUpperCase() + weekday.slice(1);
}

/** "07/11" */
export function formatDayMonth(date: Date, zone: Zone) {
	return format(date, zone, { day: "2-digit", month: "2-digit" });
}

function calendarDay(date: Date, zone: Zone) {
	return format(
		date,
		zone,
		{ year: "numeric", month: "2-digit", day: "2-digit" },
		"en-CA",
	);
}

function zoneOffsetMinutes(date: Date, zone: Zone) {
	const parts = new Intl.DateTimeFormat("en-US", {
		timeZone: TIME_ZONES[zone],
		hourCycle: "h23",
		year: "numeric",
		month: "2-digit",
		day: "2-digit",
		hour: "2-digit",
		minute: "2-digit",
		second: "2-digit",
	}).formatToParts(date);
	const get = (type: Intl.DateTimeFormatPartTypes) =>
		Number(parts.find((p) => p.type === type)?.value);
	const asUtc = Date.UTC(
		get("year"),
		get("month") - 1,
		get("day"),
		get("hour"),
		get("minute"),
		get("second"),
	);
	return (asUtc - date.getTime()) / 60_000;
}

/** Valor de `<input type="datetime-local">` no fuso informado: "2026-11-07T00:30". */
export function toZonedInput(iso: string, zone: Zone) {
	const date = new Date(iso);
	const local = new Date(
		date.getTime() + zoneOffsetMinutes(date, zone) * 60_000,
	);
	return local.toISOString().slice(0, 16);
}

/** Converte "2026-11-07T00:30" digitado no fuso informado para ISO em UTC, respeitando o horário de verão. */
export function fromZonedInput(value: string, zone: Zone) {
	const [datePart, timePart] = value.split("T");
	const [year, month, day] = datePart.split("-").map(Number);
	const [hour, minute] = timePart.split(":").map(Number);
	const guess = Date.UTC(year, month - 1, day, hour, minute);

	const offset = zoneOffsetMinutes(new Date(guess), zone);
	const utc = guess - offset * 60_000;
	// Perto da troca de horário o primeiro palpite pode cair do outro lado: confere de novo.
	const corrected = zoneOffsetMinutes(new Date(utc), zone);
	return new Date(
		corrected === offset ? utc : guess - corrected * 60_000,
	).toISOString();
}

/**
 * "00:30 Dublin · 21:30 BH". Quando o dia muda entre os fusos,
 * o dia da semana de BH aparece: "00:30 Dublin · sex 21:30 BH".
 */
export function formatDualTime(date: Date) {
	const dublin = `${formatTime(date, "dublin")} Dublin`;
	const bhTime = formatTime(date, "bh");
	const bh =
		calendarDay(date, "dublin") === calendarDay(date, "bh")
			? `${bhTime} BH`
			: `${formatWeekdayShort(date, "bh")} ${bhTime} BH`;
	return `${dublin} · ${bh}`;
}
