const express = require('express');
const favicon = require('serve-favicon');
const path = require('path');
const utils = require('./utils');
const { Configuration, OpenAIApi } = require("openai");
const { textToSpeech } = require('./az-cognitive-services/from-text-to-speech');
const { speechToText } = require('./az-cognitive-services/from-speech-to-text');
require('dotenv').config();

const key=process.env.SPEECH_KEY
const region=process.env.SPEECH_REGION

const config = new Configuration({
    apiKey: process.env.OPENAI_API_KEY,
    apiBase: process.env.OPENAI_API_KEY,
    apiType: process.env.OPENAI_API_TYPE,
    apiVersion: process.env.OPENAI_API_VERSION,
    deploymentId: process.env.OPENAI_API_DEPLOYMENT_ID
  });
const openai = new OpenAIApi(config);


const create = async () => {

    // server
    const app = express();
    app.use(favicon(path.join(__dirname, './public', 'favicon.ico')));
    // app.use(express.static(path.join(__dirname, './public/client.js')));
    
    // Log request
    app.use(utils.appLogger);

    // root route - serve static file
    app.get('/api/hello', (req, res) => {
        res.json({hello: 'goodbye'});
        res.end();
    });

    // root route - serve static file
    app.get('/', (req, res) => {
        return res.sendFile(path.join(__dirname, './public/client.html'));

    });

    app.get('/text-to-speech', async (req, res, next) => {        
        var phrase=req.query.phrase || "hello world" 
        var language= req.query.language || "ro-Ro-EmilNeural"
        if (!key || !region || !phrase) res.status(404).send('Invalid query string');
        

        /**-------------------- */
        //Ask GPT

        console.log(phrase)
        askGPT(phrase).then(async(response)=>{

            console.log(response.choices[0].text);
            const resp=response.choices[0].text;
            //Send reponse [Audio response] to client

            let fileName = null;

            fileName=`rezultat.mp3`
            const audioStream = await textToSpeech(key, region, resp,language, fileName);
            res.set({
                'Content-Type': 'audio/mpeg',
                'Transfer-Encoding': 'chunked'
            });
            audioStream.pipe(res);
            
        })
        
    });
    app.get('/speech-to-text', async (req, res, next) => { 
        var language= req.query.language || "ro-Ro";
        var audio=req.query.audio //|| "https://speechtotextdemo.blob.core.windows.net/speechtotextcontainer/ro-RO-EmilNeural-2020-10-20T15_00_00_000Z-2020-10-20T15_00_10_000Z.mp3" 
        const textFromSpeech = await speechToText(key, region, audio,language);
        res.set({
            'Content-Type': 'application/json'
        });
        res.send(textFromSpeech);
    });
/**-------------------- */

        async function askGPT(question){
    const context=""//="Cat mai multe detalii"
        try {
        if (question == null) {
            throw new Error("Uh oh, no text was provided");
        }
        const answer =await openai.createCompletion({
            model:process.env.OPENAI_MODEL,
            prompt: `${question}\n${context}\n`,
            max_tokens:300
        });
            return answer.data;
            
        } catch (error) {
        console.log(error.message);
        }
}


//create a post request to openai to get the answer for a question
app.post('/ask', async (req, res) => {
    const question = "Cate picioare are un caine?"//req.body.question;
    askGPT(question).then((answer)=>{
        res.json(answer);
    })
});




/**-------------------- */
    // Catch errors
    app.use(utils.logErrors);
    app.use(utils.clientError404Handler);
    app.use(utils.clientError500Handler);
    app.use(utils.errorHandler);

    return app;
};

module.exports = {
    create
};