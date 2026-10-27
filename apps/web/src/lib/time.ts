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
