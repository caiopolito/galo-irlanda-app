import type { Schemas } from "@galo/api-client";

/** Conteúdo da landing, como vem de GET /api/public/landing. */
export type LandingContent = Schemas["GetLandingResponse"];
export type BoardEvent = Schemas["GetLandingBoardEvent"];
export type SiteLinks = Schemas["GetLandingSiteLinks"];
