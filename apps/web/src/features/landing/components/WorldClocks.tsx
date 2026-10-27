import { Clover } from "#/components/ui/Clover";
import { formatTime } from "#/lib/time";
import { useNow } from "#/lib/use-now";
import styles from "./WorldClocks.module.css";

export function WorldClocks() {
	const now = useNow();
	const clocks = [
		{ city: "BH", time: now ? formatTime(now, "bh") : "--:--", accent: false },
		{
			city: "Dublin",
			time: now ? formatTime(now, "dublin") : "--:--",
			accent: true,
		},
	];

	return (
		<div className={styles.clocks}>
			{clocks.map((clock) => (
				<p key={clock.city} className={styles.clock}>
					<span className={styles.label}>
						{clock.accent && (
							<Clover filled size={12} className={styles.clover} />
						)}
						<span className={styles.prefix}>Agora em </span>
						{clock.city}
					</span>
					<time className={clock.accent ? styles.timeAccent : styles.time}>
						{clock.time}
					</time>
				</p>
			))}
		</div>
	);
}
