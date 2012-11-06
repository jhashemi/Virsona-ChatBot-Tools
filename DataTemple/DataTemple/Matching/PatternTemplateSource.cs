using System;
using System.Collections.Generic;
using System.Text;
using PluggerBase.ActionReaction.Evaluations;
using LanguageNet.Grammarian;
using DataTemple.AgentEvaluate;
using DataTemple.Codeland;

namespace DataTemple.Matching
{
    public class PatternTemplateSource : IContinuation
    {
        protected const double maxSalience = 100.0;
        
        protected Context pattern;
        protected Context template;
        protected double score;
        protected string source;

        // For evaluation
        protected Coderack coderack;
        protected double salience;
        protected IContinuation succ;

        public PatternTemplateSource(Context pattern, Context template, double score, string source)
        {
            this.pattern = pattern;
            this.template = template;
            this.score = score;
            this.source = source;
        }

        public PatternTemplateSource(PatternTemplateSource parent, Coderack coderack, double salience, IContinuation succ)
        {
            pattern = parent.pattern;
            template = parent.template;
            score = parent.score;
            source = parent.source;

            this.coderack = coderack;
            this.salience = salience;
            this.succ = succ;
        }

        public Context Pattern
        {
            get
            {
                return pattern;
            }
        }

        public Context Template
        {
            get
            {
                return template;
            }
        }

        public double Score
        {
            get
            {
                return score;
            }
        }

        public string Source
        {
            get
            {
                return source;
            }
        }

        public int Size
        {
            get {
                return pattern.Size + template.Size + 10 * 4;
            }
        }

        // Adds a codelet to check this input against us
        public void Generate(Coderack coderack, IParsedPhrase input, IContinuation succ, IFailure fail, double weight)
        {
            double salience = maxSalience * score * weight;

            PatternTemplateSource checker = new PatternTemplateSource(this, coderack, salience, succ);
            Matcher.MatchAgainst(salience, pattern, input, new List<IParsedPhrase>(), checker, fail);
        }

        public void Generate(Coderack coderack, IContinuation succ, IFailure fail, double weight)
        {
            PatternTemplateSource checker = new PatternTemplateSource(this, coderack, weight, succ);

            ContinuationAppender appender = new ContinuationAppender(pattern, checker);

            Evaluator eval = new Evaluator(maxSalience * weight, ArgumentMode.ManyArguments, appender, appender);
            eval.Lineage = ContinueAgentCodelet.NewLineage();
            appender.RegisterCaller(eval.Lineage);
            appender.RegisterCaller(eval.Lineage);

            eval.Continue(pattern, fail);
        }

        #region IContinuation Members

        public int Continue(object value, IFailure fail)
        {
			Context context = (Context) value;
            // Did we successfully match everything?  If so, evaluate the template
            if (context.IsEmpty || (context.Contents.Count == 1 && context.Contents[0].Name.StartsWith("*")))
            {
                Evaluator eval = new Evaluator(salience, ArgumentMode.ManyArguments, succ, new NopCallable());
                Context child = new Context(context, template.Contents);
                child.Map["$production"] = true;
                return eval.Continue(child, fail) + 1;
            }
            else
            {
                // we didn't match everything
                fail.Fail("Context is not empty", succ);
                return 1;
            }
        }
		
		public object Clone()
		{
			return new PatternTemplateSource(this, coderack, salience, succ);
		}

        #endregion
    }
}