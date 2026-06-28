// proxy-server.js
// Server Node.js minimo che fa da intermediario tra Unity e l'API Anthropic.
// Deployment consigliato: Railway, Render, o Fly.io (free tier sufficiente).
//
// PERCHÉ: non mettere mai la API key nel build Unity — è estraibile.
// Il proxy riceve richieste da Unity, aggiunge la key, e le inoltra.
//
// Setup:
//   npm init -y
//   npm install express node-fetch
//   API_KEY=sk-ant-... node proxy-server.js

const express  = require('express');
const fetch    = require('node-fetch');

const app    = express();
const PORT   = process.env.PORT || 3000;
const APIKEY = process.env.API_KEY;

if (!APIKEY) {
    console.error('ERRORE: variabile API_KEY non impostata.');
    process.exit(1);
}

app.use(express.json({ limit: '10kb' }));

// ── Whitelist modelli permessi ────────────────────────────────────────
const ALLOWED_MODELS = ['claude-sonnet-4-20250514'];

// ── Endpoint proxy ────────────────────────────────────────────────────
app.post('/v1/messages', async (req, res) => {
    try {
        const body = req.body;

        // Valida il modello
        if (!ALLOWED_MODELS.includes(body.model)) {
            return res.status(400).json({ error: 'Modello non permesso.' });
        }

        // Limita max_tokens per sicurezza
        body.max_tokens = Math.min(body.max_tokens || 300, 500);

        const response = await fetch('https://api.anthropic.com/v1/messages', {
            method:  'POST',
            headers: {
                'Content-Type':      'application/json',
                'x-api-key':         APIKEY,
                'anthropic-version': '2023-06-01'
            },
            body: JSON.stringify(body)
        });

        const data = await response.json();
        res.status(response.status).json(data);

    } catch (err) {
        console.error('Proxy error:', err);
        res.status(500).json({ error: 'Errore interno del proxy.' });
    }
});

app.get('/health', (_, res) => res.json({ ok: true }));

app.listen(PORT, () => console.log(`Proxy attivo su porta ${PORT}`));
