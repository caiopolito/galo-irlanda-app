import { FlapTile } from "#/components/ui/FlapTile";
import { HandNote } from "#/components/ui/HandNote";
import { SectionHeader } from "#/components/ui/SectionHeader";
import { StatusPill, type StatusTone } from "#/components/ui/StatusPill";
import { formatTime } from "#/lib/time";
import {
	type BoardEvent,
	boardEvents,
	CITY_NAMES,
	type EventStatus,
} from "../data";
import styles from "./DepartureBoard.module.css";
import { WorldClocks } from "./WorldClocks";

function statusDisplay(status: EventStatus): {
	tone: StatusTone;
	label: string;
} {
	switch (status.kind) {
		case "boarding":
			return { tone: "boarding", label: "Embarcando" };
		case "confirmed":
			return { tone: "confirmed", label: "Confirmado" };
		case "spots":
			return {
				tone: "muted",
				label: `${status.taken} de ${status.total} vagas`,
			};
		case "voting":
			return { tone: "pending", label: "Em votação" };
	}
}

function rowData(event: BoardEvent) {
	return {
		time: formatTime(new Date(event.startsAt), "dublin"),
		city: CITY_NAMES[event.city],
		status: statusDisplay(event.status),
		accent: event.status.kind === "boarding",
	};
}

export function DepartureBoard() {
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
						{boardEvents.map((event) => {
							const row = rowData(event);
							return (
								<tr key={event.id}>
									<td>
										<FlapTile accent={row.accent}>{row.time}</FlapTile>
									</td>
									<td>
										<FlapTile>{event.title}</FlapTile>
									</td>
									<td>
										<FlapTile>{event.venue}</FlapTile>
									</td>
									<td>
										<FlapTile>{row.city}</FlapTile>
									</td>
									<td>
										<StatusPill tone={row.status.tone} className={styles.fill}>
											{row.status.label}
										</StatusPill>
									</td>
								</tr>
							);
						})}
					</tbody>
				</table>

				<ol className={styles.cards} aria-label="Próximos jogos e encontros">
					{boardEvents.map((event) => {
						const row = rowData(event);
						return (
							<li key={event.id} className={styles.card}>
								<div className={styles.cardTiles}>
									<FlapTile size="sm" accent={row.accent}>
										{row.time}
									</FlapTile>
									<FlapTile size="sm" className={styles.grow}>
										{event.title}
									</FlapTile>
								</div>
								<div className={styles.cardMeta}>
									<span>
										{event.venue} · {row.city}
									</span>
									<StatusPill tone={row.status.tone} size="sm">
										{row.status.label}
									</StatusPill>
								</div>
							</li>
						);
					})}
				</ol>
			</div>

			<HandNote tone="soft" className={styles.note}>
				↑ chega cedo, que o pub enche rápido!
			</HandNote>
		</section>
	);
}
