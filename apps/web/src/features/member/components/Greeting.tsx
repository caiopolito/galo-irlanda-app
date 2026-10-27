import type { Schemas } from "@galo/api-client";
import styles from "../member.module.css";

function greeting(now: Date) {
	const hour = Number(
		new Intl.DateTimeFormat("en-GB", {
			timeZone: "Europe/Dublin",
			hour: "2-digit",
			hourCycle: "h23",
		}).format(now),
	);
	if (hour >= 5 && hour < 12) return "Bom dia";
	if (hour >= 12 && hour < 18) return "Boa tarde";
	return "Boa noite";
}

export function Greeting({
	me,
}: {
	me: Schemas["GetMemberDashboardResponse"];
}) {
	const since = new Intl.DateTimeFormat("pt-BR", {
		month: "short",
		year: "numeric",
		timeZone: "Europe/Dublin",
	})
		.format(new Date(me.joinedAt))
		.replace(".", "");
	const active = me.membership.isActive;

	return (
		<section
			style={{
				display: "flex",
				flexWrap: "wrap",
				alignItems: "flex-end",
				justifyContent: "space-between",
				gap: 20,
			}}
		>
			<div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
				<span
					style={{
						fontFamily: "var(--font-hand)",
						fontSize: 30,
						color: "var(--color-amber)",
						transform: "rotate(-2deg)",
						alignSelf: "flex-start",
						lineHeight: 1,
					}}
				>
					ou boa madrugada, né
				</span>
				<h1 className={styles.h1}>
					{greeting(new Date())}, {me.displayName}.
				</h1>
				<p className={styles.muted} style={{ margin: 0, fontSize: 17 }}>
					Sócio nº {String(me.memberNumber).padStart(4, "0")} · na Massa desde{" "}
					{since}
				</p>
			</div>
			<div className={styles.chips}>
				<span className={styles.chip}>
					<span
						className={`${styles.dot} ${active ? "" : styles.dotWarning}`}
					/>
					{active ? "Mensalidade em dia" : "Mensalidade pendente"}
				</span>
				<span className={styles.chip}>
					{me.attended} {me.attended === 1 ? "madrugada" : "madrugadas"} com a
					Massa
				</span>
			</div>
		</section>
	);
}
