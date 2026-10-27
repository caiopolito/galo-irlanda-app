import type { Schemas } from "@galo/api-client";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { FlapTile } from "#/components/ui/FlapTile";
import { api, apiBaseUrl, unwrap } from "#/lib/api";
import { formatDayMonth, formatTime, formatWeekdayShort } from "#/lib/time";
import styles from "../member.module.css";
import { memberKeys } from "../useMember";

type Answer = Schemas["RsvpAnswer"];

const ANSWERS: { value: Answer; label: string }[] = [
	{ value: "going", label: "Vou" },
	{ value: "maybe", label: "Talvez" },
	{ value: "not_going", label: "Dessa vez não" },
];

function useRsvp() {
	const queryClient = useQueryClient();
	return useMutation({
		mutationFn: ({ id, answer }: { id: string; answer: Answer }) =>
			unwrap(
				api.PUT("/api/socio/events/{id}/rsvp", {
					params: { path: { id } },
					body: { answer },
				}),
			),
		onSuccess: () =>
			queryClient.invalidateQueries({ queryKey: memberKeys.events }),
	});
}

function useEvents() {
	return useQuery({
		queryKey: memberKeys.events,
		queryFn: () => unwrap(api.GET("/api/socio/events")),
	});
}

/** "Seu próximo embarque" com o horário duplo e a pergunta "Você vai?". */
export function NextTrip() {
	const events = useEvents();
	const rsvp = useRsvp();
	const next = events.data?.next;

	if (!next) {
		return (
			<div className={styles.panel}>
				<span className={styles.eyebrow}>Seu próximo embarque</span>
				<p className={styles.text}>
					Nenhum jogo marcado ainda. Fica de olho no grupo!
				</p>
			</div>
		);
	}

	const start = new Date(next.startsAt);
	return (
		<div className={styles.panel}>
			<span className={styles.eyebrow}>Seu próximo embarque</span>
			<h2 className={styles.h2Big}>{next.title}</h2>
			<div className={styles.clocks}>
				<div className={styles.clock}>
					<span className={styles.clockLabel}>
						Dublin · {formatWeekdayShort(start, "dublin")}
					</span>
					<span className={styles.clockTimeAccent}>
						{formatTime(start, "dublin")}
					</span>
				</div>
				<div className={styles.clock}>
					<span className={styles.clockLabel}>
						BH · {formatWeekdayShort(start, "bh")}
					</span>
					<span className={styles.clockTime}>{formatTime(start, "bh")}</span>
				</div>
			</div>
			<p className={styles.text}>
				{next.place} · {next.cityName}.{" "}
				{next.going > 0 && `${next.going} atleticanos confirmados.`}
			</p>
			<span className={styles.eyebrow}>Você vai?</span>
			<fieldset className={styles.rsvp}>
				<legend className="sr-only">Presença</legend>
				{ANSWERS.map((option) => (
					<button
						key={option.value}
						type="button"
						aria-pressed={next.myAnswer === option.value}
						className={`${styles.rsvpOption} ${next.myAnswer === option.value ? styles.rsvpSelected : ""}`}
						disabled={rsvp.isPending}
						onClick={() => rsvp.mutate({ id: next.id, answer: option.value })}
					>
						{option.label}
						{next.myAnswer === option.value && " ✓"}
					</button>
				))}
			</fieldset>
			{rsvp.isError && (
				<span className={styles.muted}>
					Não deu para salvar. As vagas podem ter acabado.
				</span>
			)}
		</div>
	);
}

/** "Meus embarques": todos os próximos eventos com a resposta do sócio. */
export function MyTrips() {
	const events = useEvents();
	const rsvp = useRsvp();
	const calendarUrl =
		`${apiBaseUrl || (typeof window !== "undefined" ? window.location.origin : "")}/api/public/events.ics`.replace(
			/^https?:/,
			"webcal:",
		);

	return (
		<section id="jogos" className={styles.deepPanel}>
			<div className={styles.sectionHead}>
				<h2 className={styles.h2}>Meus embarques</h2>
				<a href={calendarUrl} className={styles.link}>
					Sincronizar com meu calendário
				</a>
			</div>
			{events.data?.events.length === 0 && (
				<p className={styles.text}>Nenhum evento marcado.</p>
			)}
			<ul className={styles.trips}>
				{events.data?.events.map((event) => {
					const start = new Date(event.startsAt);
					return (
						<li key={event.id} className={styles.trip}>
							<FlapTile size="sm" accent={event.myAnswer === "going"}>
								{formatWeekdayShort(start, "dublin")}{" "}
								{formatDayMonth(start, "dublin")}
							</FlapTile>
							<div className={styles.tripInfo}>
								<span className={styles.tripTitle}>{event.title}</span>
								<span className={styles.tripPlace}>
									{formatTime(start, "dublin")} · {event.place}
								</span>
							</div>
							<div
								className={styles.tripAnswer}
								role="radiogroup"
								aria-label={`Presença em ${event.title}`}
							>
								{ANSWERS.map((option) => (
									<button
										key={option.value}
										type="button"
										aria-pressed={event.myAnswer === option.value}
										className={`${styles.answerChip} ${event.myAnswer === option.value ? styles.answerChipOn : ""}`}
										disabled={rsvp.isPending}
										onClick={() =>
											rsvp.mutate({ id: event.id, answer: option.value })
										}
									>
										{option.value === "not_going" ? "Não vou" : option.label}
									</button>
								))}
							</div>
						</li>
					);
				})}
			</ul>
		</section>
	);
}
