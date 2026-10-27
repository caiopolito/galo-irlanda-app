import { BoardingPass } from "#/components/boarding-pass/BoardingPass";
import { Button } from "#/components/ui/Button";
import { HandNote } from "#/components/ui/HandNote";
import { links } from "#/config/links";
import { formatMoney } from "#/lib/money";
import type { SiteLinks } from "../types";
import styles from "./MemberCta.module.css";

function formatFee({ membershipFeeCents, membershipCurrency }: SiteLinks) {
	return membershipFeeCents === null
		? "€[valor]"
		: formatMoney(membershipFeeCents, membershipCurrency);
}

export function MemberCta({ settings }: { settings: SiteLinks }) {
	return (
		<section
			id="socio"
			className={styles.section}
			aria-labelledby="socio-title"
		>
			<div className={styles.card}>
				<HandNote className={styles.note}>vem com a gente</HandNote>
				<h2 id="socio-title" className={styles.title}>
					Seja sócio da Massa Dublin.
				</h2>
				<div className={styles.passWrapper}>
					<BoardingPass passenger="[Seu nome]" number="0001" />
				</div>
				<p className={styles.text}>
					Desconto nos pubs parceiros, lugar guardado em jogo lotado, voto nas
					decisões da torcida e a carteirinha mais bonita da Irlanda.
				</p>
				<div className={styles.actions}>
					<Button href={links.becomeMember} arrow className={styles.action}>
						Quero ser sócio
					</Button>
					<span className={styles.price}>
						a partir de {formatFee(settings)}/mês
					</span>
				</div>
			</div>
		</section>
	);
}
