import { createFileRoute } from "@tanstack/react-router";
import { Notice } from "#/features/admin/ui/Notice";
import { Billing } from "#/features/member/components/Billing";
import { BoardingCard } from "#/features/member/components/BoardingCard";
import {
	Benefits,
	Documents,
	Invite,
	TaggedPhotos,
} from "#/features/member/components/Extras";
import { Greeting } from "#/features/member/components/Greeting";
import { Polls } from "#/features/member/components/Polls";
import { MyTrips, NextTrip } from "#/features/member/components/Trips";
import styles from "#/features/member/member.module.css";
import { useMemberProfile } from "#/features/member/useMember";

export const Route = createFileRoute("/socio/")({ component: MemberDashboard });

function MemberDashboard() {
	const me = useMemberProfile();

	if (me.isPending) return <main className={styles.main}>Carregando…</main>;
	if (me.isError) {
		return (
			<main className={styles.main}>
				<Notice tone="error">
					Não encontramos sua ficha de sócio. Fale com a diretoria.
				</Notice>
			</main>
		);
	}

	return (
		<main className={styles.main}>
			<Greeting me={me.data} />
			<div className={styles.row}>
				<BoardingCard />
				<NextTrip />
			</div>
			<MyTrips />
			<div className={styles.row}>
				<Billing />
				<Polls />
			</div>
			<Benefits />
			<div className={styles.row}>
				<TaggedPhotos />
				<Documents />
			</div>
			<Invite
				referralCode={me.data.referralCode}
				referrals={me.data.referrals}
			/>
			<p className={styles.legal}>
				Torcida independente. Sem vínculo oficial com o Clube Atlético Mineiro.
			</p>
		</main>
	);
}
