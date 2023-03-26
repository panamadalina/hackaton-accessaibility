const url = 'https://madalinaopenai.openai.azure.com/';
// const params = {
//   "prompt": prompt,
//   "max_tokens": 160,
//   "temperature": 0.7,
//   "frequency_penalty": 0.5
// };
const headers = {
  'Authorization': `Bearer a4a13792e87449ec81108181061ca8f6`,
};

    const configuration = new Configuration({
        apiKey:"a4a13792e87449ec81108181061ca8f6",// process.env.OPENAI_API_KEY,
        apiBase :  "https://madalinaopenai.openai.azure.com/",
        apiType:'azure',
        apiVersion : "2020-08-03",
        deploymentId:"azure-openai-davinci"
      });

      const openai = new OpenAIApi(configuration);
      app.post("/ask", async (req, res) => {
        const prompt = "Cate grade vor fi maine?"//req.body.prompt;
        try {
          if (prompt == null) {
            throw new Error("Uh oh, no prompt was provided");
          }
          const response = await openai.createCompletion({
            model: "text-davinci-003",
            prompt,
          });
          const completion = response.data.choices[0].text;
          return res.status(200).json({
            success: true,
            message: completion,
          });
        } catch (error) {
          console.log(error.message);
        }
      });