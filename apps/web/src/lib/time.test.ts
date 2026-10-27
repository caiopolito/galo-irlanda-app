import { describe, expect, it } from "vitest";
import {
	formatDayMonth,
	formatDualTime,
	formatTime,
	formatWeekdayLong,
	formatWeekdayShort,
	fromZonedInput,
	toZonedInput,
} from "./time";

describe("formatDualTime", () => {
	it("shows the BH weekday when the day differs (Irish winter time, UTC+0)", () => {
		expect(formatDualTime(new Date("2026-11-07T00:30:00Z"))).toBe(
			"00:30 Dublin · sex 21:30 BH",
		);
	});

	it("omits the BH weekday on the same day", () => {
		expect(formatDualTime(new Date("2026-11-08T14:00:00Z"))).toBe(
			"14:00 Dublin · 11:00 BH",
		);
	});

	it("follows Irish summer time (UTC+1) without a fixed offset", () => {
		expect(formatDualTime(new Date("2026-07-04T19:00:00Z"))).toBe(
			"20:00 Dublin · 16:00 BH",
		);
	});

	it("handles the night the clocks go back in Ireland", () => {
		// 25/10/2026 01:30 UTC = 01:30 GMT (already after the change at 01:00 UTC)
		expect(formatTime(new Date("2026-10-25T01:30:00Z"), "dublin")).toBe(
			"01:30",
		);
		// 25/10/2026 00:30 UTC = 01:30 IST (still before the change)
		expect(formatTime(new Date("2026-10-25T00:30:00Z"), "dublin")).toBe(
			"01:30",
		);
	});
});

describe("date labels", () => {
	const kickoff = new Date("2026-11-07T00:30:00Z");

	it("formats weekdays without abbreviation dots or '-feira'", () => {
		expect(formatWeekdayShort(kickoff, "dublin")).toBe("sáb");
		expect(formatWeekdayLong(kickoff, "dublin")).toBe("Sábado");
		expect(formatWeekdayLong(new Date("2026-11-09T12:00:00Z"), "dublin")).toBe(
			"Segunda",
		);
	});

	it("formats day and month in the given zone", () => {
		expect(formatDayMonth(kickoff, "dublin")).toBe("07/11");
		expect(formatDayMonth(kickoff, "bh")).toBe("06/11");
	});
});

describe("datetime-local in Dublin time", () => {
	it("converts winter time (UTC+0) both ways", () => {
		expect(fromZonedInput("2026-11-07T00:30", "dublin")).toBe(
			"2026-11-07T00:30:00.000Z",
		);
		expect(toZonedInput("2026-11-07T00:30:00Z", "dublin")).toBe(
			"2026-11-07T00:30",
		);
	});

	it("converts summer time (UTC+1) both ways", () => {
		expect(fromZonedInput("2026-07-04T20:00", "dublin")).toBe(
			"2026-07-04T19:00:00.000Z",
		);
		expect(toZonedInput("2026-07-04T19:00:00Z", "dublin")).toBe(
			"2026-07-04T20:00",
		);
	});

	it("handles the day the clocks change", () => {
		expect(fromZonedInput("2026-03-29T12:00", "dublin")).toBe(
			"2026-03-29T11:00:00.000Z",
		);
		expect(fromZonedInput("2026-10-25T12:00", "dublin")).toBe(
			"2026-10-25T12:00:00.000Z",
		);
	});
});
