import { Crest } from "#/components/ui/Crest";
import { cx } from "#/lib/cx";
import styles from "./BoardingPass.module.css";

interface BoardingPassProps {
	passenger: string;
	number: string;
	seat?: string;
	className?: string;
}

/** Carteirinha de sócio em formato de cartão de embarque. Reaproveitada na área do sócio. */
export function BoardingPass({
	passenger,
	number,
	seat = "Massa",
	className,
}: BoardingPassProps) {
	return (
		<div className={cx(styles.pass, className)}>
			<div className={styles.main}>
				<div className={styles.top}>
					<span className={styles.club}>Galo Irlanda</span>
					<span className={styles.label}>SÓCIO</span>
				</div>
				<div className={styles.route}>
					<span>DUB</span>
					<span aria-hidden="true" className={styles.plane}>
						✈
					</span>
					<span>CNF</span>
				</div>
				<dl className={styles.fields}>
					<div>
						<dt>Passageiro</dt>
						<dd>{passenger}</dd>
					</div>
					<div>
						<dt>Assento</dt>
						<dd>{seat}</dd>
					</div>
				</dl>
			</div>
			<div className={styles.stub}>
				<Crest height={44} />
				<span className={styles.numberLabel}>Nº</span>
				<span className={styles.number}>{number}</span>
			</div>
		</div>
	);
}
