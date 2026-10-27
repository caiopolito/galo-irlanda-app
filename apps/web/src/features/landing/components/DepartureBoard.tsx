import { FlapTile } from "#/components/ui/FlapTile";
import { HandNote } from "#/components/ui/HandNote";
import { SectionHeader } from "#/components/ui/SectionHeader";
import { StatusPill } from "#/components/ui/StatusPill";
import { formatTime } from "#/lib/time";
import { useNow } from "#/lib/use-now";
import { boardStatus } from "../board-status";
import type { BoardEvent } from "../types";
import styles from "./DepartureBoard.module.css";
import { WorldClocks } from "./WorldClocks";

export function DepartureBoard({ events }: { events: BoardEvent[] }) {
	const now = useNow();
	const rows = events.map((event) => ({
		event,
		time: formatTime(new Date(event.startsAt), "dublin"),
		status: boardStatus(event, now),
	}));

	return (
		<section
			id="partidas"
			className={styles.section}
			aria-labelledby="partidas-title"
		>
			<SectionHeader
				id="partidas-title"
				title="Painel de partidas"
				subtitle="Onde a Massa se encontra nas próximas semanas."
				aside={<WorldClocks />}
			/>

			<div className={styles.board}>
				{rows.length === 0 ? (
					<p className={styles.empty}>
						Nenhum embarque marcado. Fica de olho no grupo!
					</p>
				) : (
					<>
						<table className={styles.table}>
							<caption className="sr-only">
								Próximos jogos e encontros, com horário da Irlanda
							</caption>
							<colgroup>
								<col className={styles.colTime} />
								<col className={styles.colTitle} />
								<col className={styles.colVenue} />
								<col className={styles.colCity} />
								<col className={styles.colStatus} />
							</colgroup>
							<thead>
								<tr>
									<th scope="col">Hora (IRL)</th>
									<th scope="col">Jogo / encontro</th>
									<th scope="col">Onde</th>
									<th scope="col">Cidade</th>
									<th scope="col">Situação</th>
								</tr>
							</thead>
							<tbody>
								{rows.map(({ event, time, status }) => (
									<tr key={event.id}>
										<td>
											<FlapTile accent={status.boarding}>{time}</FlapTile>
										</td>
										<td>
											<FlapTile>{event.title}</FlapTile>
										</td>
										<td>
											<FlapTile>{event.place}</FlapTile>
										</td>
										<td>
											<FlapTile>{event.cityName}</FlapTile>
										</td>
										<td>
											<StatusPill tone={status.tone} className={styles.fill}>
												{status.label}
											</StatusPill>
										</td>
									</tr>
								))}
							</tbody>
						</table>

						<ol
							className={styles.cards}
							aria-label="Próximos jogos e encontros"
						>
							{rows.map(({ event, time, status }) => (
								<li key={event.id} className={styles.card}>
									<div className={styles.cardTiles}>
										<FlapTile size="sm" accent={status.boarding}>
											{time}
										</FlapTile>
										<FlapTile size="sm" className={styles.grow}>
											{event.title}
										</FlapTile>
									</div>
									<div className={styles.cardMeta}>
										<span>
											{event.place} · {event.cityName}
										</span>
										<StatusPill tone={status.tone} size="sm">
											{status.label}
										</StatusPill>
									</div>
								</li>
							))}
						</ol>
					</>
				)}
			</div>

			<HandNote tone="soft" className={styles.note}>
				↑ chega cedo, que o pub enche rápido!
			</HandNote>
		</section>
	);
}
