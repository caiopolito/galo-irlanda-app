import { createFileRoute } from "@tanstack/react-router";
import { SiteFooter } from "#/components/layout/SiteFooter";
import { SiteHeader } from "#/components/layout/SiteHeader";
import { Cities } from "#/features/landing/components/Cities";
import { DepartureBoard } from "#/features/landing/components/DepartureBoard";
import { Hero } from "#/features/landing/components/Hero";
import { MemberCta } from "#/features/landing/components/MemberCta";
import { News } from "#/features/landing/components/News";
import { NextMatch } from "#/features/landing/components/NextMatch";
import { PhotoWall } from "#/features/landing/components/PhotoWall";
import { Timeline } from "#/features/landing/components/Timeline";

const TITLE = "Galo Irlanda · O Galo nunca joga sozinho";
const DESCRIPTION =
	"Torcida de atleticanos em Dublin, Cork e Galway. Jogos no pub, pelada, churrasco e uma saudade danada de BH.";

export const Route = createFileRoute("/")({
	head: () => ({
		meta: [
			{ title: TITLE },
			{ name: "description", content: DESCRIPTION },
			{ property: "og:type", content: "website" },
			{ property: "og:locale", content: "pt_BR" },
			{ property: "og:title", content: TITLE },
			{ property: "og:description", content: DESCRIPTION },
		],
	}),
	component: Landing,
});

function Landing() {
	return (
		<>
			<SiteHeader />
			<main>
				<Hero />
				<DepartureBoard />
				<NextMatch />
				<Cities />
				<News />
				<PhotoWall />
				<Timeline />
				<MemberCta />
			</main>
			<SiteFooter />
		</>
	);
}
