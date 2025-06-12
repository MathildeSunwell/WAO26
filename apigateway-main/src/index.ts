import fs from "fs";
import express from "express";
import cookieParser from "cookie-parser";
import { OAuth2Client } from "google-auth-library";
import { createProxyMiddleware } from "http-proxy-middleware";

interface ServiceMap {
  [route: string]: string;
}
const services: ServiceMap = JSON.parse(
  fs.readFileSync("/etc/config/services.json", "utf8")
);

const basePath = process.env.BASE_PATH || "";
const isProd = process.env.NODE_ENV !== "development";

const GOOGLE_CLIENT_ID = process.env.GOOGLE_CLIENT_ID ?? "";
if (!GOOGLE_CLIENT_ID) throw new Error("GOOGLE_CLIENT_ID env missing");
const GOOGLE_CLIENT_SECRET = process.env.GOOGLE_CLIENT_SECRET ?? "";
if (!GOOGLE_CLIENT_SECRET) throw new Error("GOOGLE_CLIENT_SECRET env missing");

const app = express();
app.set("trust proxy", 1);

app.use(cookieParser());
app.use(express.json());
app.use(express.urlencoded({ extended: true }));

const verifier = new OAuth2Client(GOOGLE_CLIENT_ID, GOOGLE_CLIENT_SECRET);
const api = express.Router();

api.post("/login/google", async (req, res) => {
  try {
    const { credential: idToken } = req.body as { credential?: string };
    if (!idToken) {
      res.status(400).send("Missing credential field");
      return;
    }

    const ticket = await verifier.verifyIdToken({
      idToken,
      audience: GOOGLE_CLIENT_ID,
    });
    const payload = ticket.getPayload();
    if (!payload) {
      res.sendStatus(401);
      return;
    }

    res.cookie("id_token", idToken, {
      httpOnly: true,
      secure: isProd,
      sameSite: "lax",
      maxAge: 60 * 60 * 1000,
      path: "/",
    });

    if (payload.name) {
      res.cookie("display_name", payload.name, {
        secure: isProd,
        sameSite: "lax",
        maxAge: 60 * 60 * 1000,
        path: "/",
      });
    }

    const returnTo = (req.query.returnTo as string) || "/";
    res.redirect(returnTo);
  } catch (err) {
    console.error("/login/google verify failed", err);
    res.sendStatus(401);
  }
});

function authenticateGoogleJWT(
  req: express.Request,
  res: express.Response,
  next: express.NextFunction
) {
  const token =
    (req.cookies as any).id_token ??
    (req.headers.authorization?.startsWith("Bearer ")
      ? req.headers.authorization.slice(7)
      : undefined);

  if (!token) {
    // XHR/fetch gets 401; browser nav gets redirect so GIS can kick in
    if (req.accepts("json")) {
      res.sendStatus(401);
      return;
    }
    const loginUrl =
      `${basePath}/login/google?returnTo=` +
      encodeURIComponent(req.originalUrl);
    res.redirect(loginUrl);
    return;
  }

  verifier
    .verifyIdToken({ idToken: token, audience: GOOGLE_CLIENT_ID })
    .then((ticket) => {
      req.user = ticket.getPayload() as any;
      (req as any).idToken = token; // keep around for onProxyReq if needed
      next();
    })
    .catch((err) => {
      console.error("JWT verify error", err);
      res.sendStatus(401);
    });
}

api.get("/auth/me", authenticateGoogleJWT, (req, res) => {
  res.json({ user: req.user });
});

for (const [route, target] of Object.entries(services)) {
  api.use(
    route,
    authenticateGoogleJWT,
    createProxyMiddleware({
      target,
      changeOrigin: true,
      pathRewrite: { [`^${route}`]: "" },
      on: {
        proxyReq: (proxyReq, req: any) => {
          const u = req.user as any;
          if (u?.email) proxyReq.setHeader("x-user-email", u.email);
          if (u?.name) proxyReq.setHeader("x-user-name", u.name);
        },
      },
    })
  );
}

api.get("/healthz", (_req, res) => {
  res.send("OK");
});

app.use(basePath, api);

app.listen(8080, () => console.log(`Gateway listening on port 8080`));
