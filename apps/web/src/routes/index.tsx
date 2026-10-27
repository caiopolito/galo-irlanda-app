import { createFileRoute } from "@tanstack/react-router";
import { SiteFooter } from "#/components/layout/SiteFooter";
import { SiteHeader } from "#/components/layout/SiteHeader";
import { externalLinks } from "#/config/links";
import { Notice } from "#/features/admin/ui/Notice";
import { Cities } from "#/features/landing/components/Cities";
import { DepartureBoard } from "#/features/landing/components/DepartureBoard";
import { Hero } from "#/features/landing/components/Hero";
import { MemberCta } from "#/features/landing/components/MemberCta";
import { News } from "#/features/landing/components/News";
import { NextMatch } from "#/features/landing/components/NextMatch";
import { PhotoWall } from "#/features/landing/components/PhotoWall";
import { Timeline } from "#/features/landing/components/Timeline";
import { api, unwrap } from "#/lib/api";

const TITLE = "Galo Irlanda · O Galo nunca joga sozinho";
const DESCRIPTION =
	"Torcida de atleticanos em Dublin, Cork e Galway. Jogos no pub, pelada, churrasco e uma saudade danada de BH.";

export const Route = createFileRoute("/")({
	// Conteúdo editado no admin (GET /api/public/landing), lido na renderização da página.
	loader: () => unwrap(api.GET("/api/public/landing")),
	head: ({ loaderData }) => {
		const image = loaderData?.hero
			? `${loaderData.hero.baseUrl}1024.webp`
			: undefined;
		return {
			meta: [
				{ title: TITLE },
				{ name: "description", content: DESCRIPTION },
				{ property: "og:type", content: "website" },
				{ property: "og:locale", content: "pt_BR" },
				{ property: "og:title", content: TITLE },
				{ property: "og:description", content: DESCRIPTION },
				...(image ? [{ property: "og:image", content: image }] : []),
			],
		};
	},
	component: Landing,
	errorComponent: LandingError,
});

function Landing() {
	const content = Route.useLoaderData();
	const external = externalLinks(content.settings);

	return (
		<>
			<SiteHeader external={external} />
			<main>
				<Hero photo={content.hero} nextMatch={content.nextMatch} />
				<DepartureBoard events={content.events} />
				{content.nextMatch && <NextMatch match={content.nextMatch} />}
				<Cities cities={content.cities} />
				<News news={content.news} />
				<PhotoWall photos={content.wallPhotos} />
				<Timeline milestones={content.milestones} />
				<MemberCta settings={content.settings} />
			</main>
			<SiteFooter external={external} />
		</>
	);
}

function LandingError() {
	return (
		<>
			<SiteHeader />
			<main
				style={{
					maxWidth: "var(--container-max)",
					margin: "0 auto",
					padding: "64px var(--gutter)",
				}}
			>
				<Notice tone="error">
					Não conseguimos carregar a página agora. Tente de novo em instantes.
				</Notice>
			</main>
			<SiteFooter />
		</>
	);
}
